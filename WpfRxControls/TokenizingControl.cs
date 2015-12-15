using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace WpfRxControls
{

    public delegate void TokenChangedDelegate(object sender, IAutoCompleteQueryResult token);

    public class TokenizingControl : RichTextBox
    {
        public static readonly DependencyProperty TokenTemplateProperty =
            DependencyProperty.Register("TokenTemplate", typeof(DataTemplate), typeof(TokenizingControl));


        public event TokenChangedDelegate TokenChanged;

        public DataTemplate TokenTemplate
        {
            get { return (DataTemplate)GetValue(TokenTemplateProperty); }
            set { SetValue(TokenTemplateProperty, value); }
        }

        public Func<string, object> TokenMatcher { get; set; }

        public TokenizingControl()
        {
            TextChanged += OnTokenTextChanged;
            KeyDown += OnStart;
        }

        private IAutoCompleteQueryResult _selectedToken;

        public IAutoCompleteQueryResult SelectedToken {  set
            {
                _selectedToken = value;
                if( TokenChanged != null )
                {
                    TokenChanged(this, value);
                }

                FlowDocument doc = new FlowDocument();
                if (value != null)
                {
                    var tokenContainer = CreateTokenContainer(value.Title, value.Title);
                    Paragraph p = new Paragraph();
                    doc.Blocks.Add(p);

                    p.Inlines.Add(tokenContainer);
                } else
                {
                    Paragraph p = new Paragraph(new Run());
                    doc.Blocks.Add(p);
                }
                Document = doc;
            }
            get
            {
                return _selectedToken;
            }
        }

        private void OnStart(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
                return;
        

            if (_selectedToken != null)
            {
                // Single token only.
                SelectedToken = null;
            }
        }

        private void OnTokenTextChanged(object sender, TextChangedEventArgs e)
        {
           
            var text = CaretPosition.GetTextInRun(LogicalDirection.Backward);
            if (TokenMatcher != null)
            {
                var token = TokenMatcher(text);
                if (token != null)
                {
                    ReplaceTextWithToken(text, token);
                }
            }
        }

        private void ReplaceTextWithToken(string inputText, object token)
        {
            // Remove the handler temporarily as we will be modifying tokens below, causing more TextChanged events
            TextChanged -= OnTokenTextChanged;

            var para = CaretPosition.Paragraph;

            var matchedRun = para.Inlines.FirstOrDefault(inline =>
            {
                var run = inline as Run;
                return (run != null && run.Text.EndsWith(inputText));
            }) as Run;
            if (matchedRun != null) // Found a Run that matched the inputText
            {
                var tokenContainer = CreateTokenContainer(inputText, token);
                para.Inlines.InsertBefore(matchedRun, tokenContainer);

                // Remove only if the Text in the Run is the same as inputText, else split up
                if (matchedRun.Text == inputText)
                {
                    para.Inlines.Remove(matchedRun);
                }
                else // Split up
                {
                    var index = matchedRun.Text.IndexOf(inputText) + inputText.Length;
                    var tailEnd = new Run(matchedRun.Text.Substring(index));
                    para.Inlines.InsertAfter(matchedRun, tailEnd);
                    para.Inlines.Remove(matchedRun);
                }
            }

            TextChanged += OnTokenTextChanged;
        }

        private InlineUIContainer CreateTokenContainer(string inputText, object token)
        {
            // Note: we are not using the inputText here, but could be used in future

            var presenter = new ContentPresenter()
            {
                Content = token,
                ContentTemplate = TokenTemplate,
            };

            // BaselineAlignment is needed to align with Run
            return new InlineUIContainer(presenter) { BaselineAlignment = BaselineAlignment.TextBottom };
        }


    }
}
