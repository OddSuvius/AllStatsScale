using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace AllStatsScale
{
    // Config row for a percent setting: drag the slider for 1-1000%, or click the box and type any value (bigger values pin the slider to its end).
    // tModLoader's own slider and number box are internal, so this combines its public RangeElement (the slider) with a small text box.
    public class PercentElement : RangeElement
    {
        private const int SliderMax = 1000;
        private NumberBox box;

        private int Percent
        {
            get => (int)GetObject();
            set => SetObject(Math.Max(1, value));
        }

        public override int NumberTicks => 0;
        public override float TickIncrement => 0f;

        protected override float Proportion
        {
            get => Math.Min(1f, (Percent - 1) / (float)(SliderMax - 1));
            set
            {
                Percent = (int)Math.Round(value * (SliderMax - 1)) + 1;
                box.Text = Percent.ToString();
            }
        }

        public override void OnBind()
        {
            base.OnBind();
            TextDisplayFunction = () => $"{Label}: {Percent}%";

            var panel = new UIPanel();
            panel.SetPadding(0f);
            panel.Left.Set(-(10f + 180f + 8f + 80f), 1f); // just left of the slider, which RangeElement draws 10px from the right edge, about 180px wide
            panel.Top.Set(2f, 0f);
            panel.Width.Set(80f, 0f);
            panel.Height.Set(26f, 0f);
            Append(panel);

            box = new NumberBox { Text = Percent.ToString() };
            box.Left.Set(8f, 0f);
            box.Top.Set(3f, 0f);
            box.Width.Set(-8f, 1f);
            box.Height.Set(0f, 1f);
            box.OnTextChanged += text =>
            {
                if (int.TryParse(text, out int value))
                    Percent = value;
            };
            box.OnUnfocus += () => box.Text = Percent.ToString(); // drop anything that wasn't a number
            panel.Append(box);
        }
    }

    // Minimal click-to-type text box, modeled on tModLoader's internal UIFocusInputTextField. Enter, Escape or clicking elsewhere finishes.
    internal class NumberBox : UIElement
    {
        public string Text = "";
        public event Action<string> OnTextChanged;
        public event Action OnUnfocus;
        private bool focused;

        public override void LeftClick(UIMouseEvent evt)
        {
            Main.clrInput();
            focused = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (focused && Main.mouseLeft && !ContainsPoint(Main.MouseScreen))
                Unfocus();
            base.Update(gameTime);
        }

        private void Unfocus()
        {
            focused = false;
            OnUnfocus?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (focused)
            {
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();
                string input = Main.GetInputText(Text);
                if (input != Text)
                {
                    Text = input;
                    OnTextChanged?.Invoke(Text);
                }
                if (Main.inputTextEnter || Main.inputTextEscape)
                {
                    Main.inputTextEscape = false;
                    Unfocus();
                }
            }

            bool caret = focused && Main.GlobalTimeWrappedHourly % 1f < 0.5f;
            CalculatedStyle d = GetDimensions();
            Utils.DrawBorderString(spriteBatch, caret ? Text + "|" : Text, new Vector2(d.X, d.Y), Color.White);
        }
    }
}
