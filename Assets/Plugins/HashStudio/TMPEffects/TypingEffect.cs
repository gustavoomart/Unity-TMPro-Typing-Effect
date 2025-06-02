namespace HashStudio.TMProEffects {
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
    /// <summary>
    /// A typing effect component for TextMeshPro that simulates typewriter-style text animation.
    /// Supports rich text formatting, customizable timing, optional blinking caret, and asynchronous playback.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypingEffect : MonoBehaviour {

        #region Serialized Fields

        [Header("Typing Settings")]
        /// <summary>
        /// The total time it takes to type the entire text, regardless of text length.
        /// </summary>
        [SerializeField] private float totalTypingTime = 2f;

        /// <summary>
        /// Random variation applied to individual character timing (0-1 range).
        /// Higher values create more irregular typing rhythm while maintaining total time.
        /// </summary>
        [SerializeField] private float noiseVariation = 0.3f;

        [Header("Caret Settings")]
        /// <summary>
        /// Whether to show a blinking caret cursor during and/or after typing.
        /// </summary>
        [SerializeField] private bool showCaret = true;

        /// <summary>
        /// The character used as the blinking caret cursor.
        /// </summary>
        [SerializeField] private char caretChar = '|';

        /// <summary>
        /// How fast the caret blinks (time between blink states in seconds).
        /// </summary>
        [SerializeField] private float caretBlinkRate = 0.5f;

        /// <summary>
        /// Whether the caret should continue blinking after typing is complete.
        /// </summary>
        [SerializeField] private bool keepCaretAfterTyping = true;

        #endregion

        #region Private Fields

        private TMP_Text textComponent;
        private Coroutine typingCoroutine;
        private Coroutine caretCoroutine;

        private string currentMessage;
        private string processedText;
        private List<RichTextTag> richTextTags;
        private bool isTyping = false;

        /// <summary>
        /// Structure to hold rich text tag information for proper rendering during typing.
        /// </summary>
        private struct RichTextTag {
            public int position;
            public string tag;
            public bool isClosing;

            public RichTextTag(int pos, string tagText, bool closing) {
                position = pos;
                tag = tagText;
                isClosing = closing;
            }
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Initialize components and collections.
        /// </summary>
        private void Awake() {
            EnsureInitialized();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets whether the typing effect is currently active.
        /// </summary>
        /// <returns>True if text is currently being typed, false otherwise.</returns>
        public bool IsTyping() => isTyping;

        /// <summary>
        /// Starts typing the specified message with the configured settings.
        /// This is a fire-and-forget method that doesn't wait for completion.
        /// </summary>
        /// <param name="message">The text to type, supports TextMeshPro rich text formatting.</param>
        public void PlayText(string message) {
            EnsureInitialized();
            if (typingCoroutine != null) {
                StopCoroutine(typingCoroutine);
            }

            if (caretCoroutine != null) {
                StopCoroutine(caretCoroutine);
            }

            currentMessage = message;
            ProcessRichText();
            typingCoroutine = StartCoroutine(TypeText());
        }

        /// <summary>
        /// Asynchronously types the specified message and waits for completion.
        /// Use this method when you need to wait for the typing to finish before continuing.
        /// </summary>
        /// <param name="message">The text to type, supports TextMeshPro rich text formatting.</param>
        /// <returns>A task that completes when the typing animation finishes.</returns>
        public async Task PlayTextAsync(string message) {
            EnsureInitialized();
            if (typingCoroutine != null) {
                StopCoroutine(typingCoroutine);
            }

            if (caretCoroutine != null) {
                StopCoroutine(caretCoroutine);
            }

            currentMessage = message;
            ProcessRichText();

            isTyping = true;
            typingCoroutine = StartCoroutine(TypeText());

            while (isTyping && this != null) {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Immediately stops the current typing animation and displays the complete text.
        /// </summary>
        public void StopTyping() {
            if (typingCoroutine != null) {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (caretCoroutine != null) {
                StopCoroutine(caretCoroutine);
                caretCoroutine = null;
            }

            isTyping = false;
            textComponent.text = currentMessage;
        }

        /// <summary>
        /// Waits for the current typing animation to complete if one is active.
        /// </summary>
        /// <returns>A task that completes when the current typing finishes.</returns>
        public async Task WaitForCurrentText() {
            while (isTyping && this != null) {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Plays multiple text messages in sequence, waiting for each to complete before starting the next.
        /// Uses the default caret blink rate as pause duration between messages.
        /// </summary>
        /// <param name="messages">Array of text messages to play sequentially.</param>
        /// <returns>A task that completes when all messages have been typed.</returns>
        public async Task PlayTextsSequentially(params string[] messages) {
            EnsureInitialized();
            foreach (string message in messages) {
                await PlayTextAsync(message);
                await Task.Delay((int)(caretBlinkRate * 1000));
            }
        }

        /// <summary>
        /// Plays multiple text messages in sequence with custom pause duration between messages.
        /// </summary>
        /// <param name="pauseBetweenTexts">Time to wait between each message in seconds.</param>
        /// <param name="messages">Array of text messages to play sequentially.</param>
        /// <returns>A task that completes when all messages have been typed.</returns>
        public async Task PlayTextsSequentially(float pauseBetweenTexts, params string[] messages) {
            EnsureInitialized();
            for (int i = 0; i < messages.Length; i++) {
                await PlayTextAsync(messages[i]);

                if (i < messages.Length - 1) {
                    await Task.Delay((int)(pauseBetweenTexts * 1000));
                }
            }
        }

        /// <summary>
        /// Sets the total time for typing animation. The text will always complete within this duration.
        /// </summary>
        /// <param name="time">Total typing time in seconds (minimum 0.1 seconds).</param>
        public void SetTotalTypingTime(float time) {
            totalTypingTime = Mathf.Max(0.1f, time);
        }

        /// <summary>
        /// Sets the random variation applied to individual character timing.
        /// </summary>
        /// <param name="noise">Noise variation amount (0-1 range, where 0 = no variation, 1 = maximum variation).</param>
        public void SetNoiseVariation(float noise) {
            noiseVariation = Mathf.Clamp01(noise);
        }

        /// <summary>
        /// Sets the character used for the blinking caret cursor.
        /// </summary>
        /// <param name="caret">The character to use as caret cursor.</param>
        public void SetCaretChar(char caret) {
            caretChar = caret;
        }

        /// <summary>
        /// Sets how fast the caret blinks.
        /// </summary>
        /// <param name="rate">Blink rate in seconds (minimum 0.1 seconds).</param>
        public void SetCaretBlinkRate(float rate) {
            caretBlinkRate = Mathf.Max(0.1f, rate);
        }

        /// <summary>
        /// Enables or disables the caret cursor entirely.
        /// </summary>
        /// <param name="show">True to show caret, false to hide it completely.</param>
        public void SetShowCaret(bool show) {
            showCaret = show;
        }

        /// <summary>
        /// Sets whether the caret should continue blinking after typing is complete.
        /// </summary>
        /// <param name="keep">True to keep caret blinking after typing, false to hide it when done.</param>
        public void SetKeepCaretAfterTyping(bool keep) {
            keepCaretAfterTyping = keep;

            if (!keep && !isTyping && caretCoroutine != null) {
                StopCoroutine(caretCoroutine);
                caretCoroutine = null;
                textComponent.text = GetTextWithRichTags(processedText.Length);
            }
        }

        /// <summary>
        /// Immediately hides the caret cursor if it's currently visible.
        /// </summary>
        public void HideCaret() {
            if (caretCoroutine != null) {
                StopCoroutine(caretCoroutine);
                caretCoroutine = null;
            }

            if (!isTyping) {
                textComponent.text = GetTextWithRichTags(processedText.Length);
            }
        }

        #endregion

        #region Private Methods

        private void EnsureInitialized() {
            if (textComponent == null || richTextTags == null)
                textComponent = GetComponent<TMP_Text>();
            richTextTags = new List<RichTextTag>();
        }

        /// <summary>
        /// Processes the input text to separate rich text tags from displayable characters.
        /// This allows the typing effect to work properly with formatted text.
        /// </summary>
        private void ProcessRichText() {
            richTextTags.Clear();
            processedText = "";

            string pattern = @"</?[^>]+>";
            MatchCollection matches = Regex.Matches(currentMessage, pattern);

            int textPosition = 0;
            int lastIndex = 0;

            foreach (Match match in matches) {
                string textBeforeTag = currentMessage.Substring(lastIndex, match.Index - lastIndex);
                processedText += textBeforeTag;

                bool isClosing = match.Value.StartsWith("</");
                richTextTags.Add(new RichTextTag(textPosition + textBeforeTag.Length, match.Value, isClosing));

                textPosition += textBeforeTag.Length;
                lastIndex = match.Index + match.Length;
            }

            processedText += currentMessage.Substring(lastIndex);
        }

        /// <summary>
        /// Main coroutine that handles the typing animation logic.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator TypeText() {
            isTyping = true;
            textComponent.text = "";

            if (showCaret) {
                StartCaretBlink();
            }

            int visibleCharacters = 0;
            int totalCharacters = processedText.Length;

            if (totalCharacters == 0) {
                isTyping = false;
                yield break;
            }

            float baseCharTime = totalTypingTime / totalCharacters;

            while (visibleCharacters < totalCharacters) {
                float randomVariation = Random.Range(-noiseVariation, noiseVariation);
                float charTime = Mathf.Max(0.01f, baseCharTime + (randomVariation * baseCharTime));

                if (visibleCharacters == totalCharacters - 1) {
                    float remainingTime = totalTypingTime - (Time.time - (Time.time - totalTypingTime));
                    charTime = Mathf.Max(0.01f, remainingTime);
                }

                yield return new WaitForSeconds(charTime);

                visibleCharacters++;
                UpdateDisplayedText(visibleCharacters);
            }

            isTyping = false;

            if (showCaret && !keepCaretAfterTyping) {
                if (caretCoroutine != null) {
                    StopCoroutine(caretCoroutine);
                    caretCoroutine = null;
                }
                textComponent.text = GetTextWithRichTags(visibleCharacters);
            }
            else if (showCaret && keepCaretAfterTyping) {
                caretCoroutine = StartCoroutine(BlinkCaretAfterTyping());
            }
        }

        /// <summary>
        /// Updates the displayed text with the current number of visible characters and caret.
        /// </summary>
        /// <param name="visibleChars">Number of characters that should be visible.</param>
        private void UpdateDisplayedText(int visibleChars) {
            string displayText = GetTextWithRichTags(visibleChars);

            if (isTyping && showCaret) {
                displayText += caretChar;
            }

            textComponent.text = displayText;
        }

        /// <summary>
        /// Reconstructs the text with proper rich text tags for the specified number of visible characters.
        /// Ensures that formatting tags are properly opened and closed.
        /// </summary>
        /// <param name="visibleChars">Number of characters to include in the output.</param>
        /// <returns>Formatted text string with proper rich text tags.</returns>
        private string GetTextWithRichTags(int visibleChars) {
            string result = "";
            int textIndex = 0;

            Stack<string> openTags = new Stack<string>();

            for (int i = 0; i < processedText.Length && textIndex < visibleChars; i++) {
                foreach (var tag in richTextTags) {
                    if (tag.position == textIndex) {
                        if (tag.isClosing) {
                            result += tag.tag;
                            if (openTags.Count > 0)
                                openTags.Pop();
                        }
                        else {
                            result += tag.tag;
                            openTags.Push(tag.tag);
                        }
                    }
                }

                if (textIndex < visibleChars) {
                    result += processedText[i];
                    textIndex++;
                }
            }

            while (openTags.Count > 0) {
                string openTag = openTags.Pop();
                string tagName = ExtractTagName(openTag);
                result += "</" + tagName + ">";
            }

            return result;
        }

        /// <summary>
        /// Extracts the tag name from a rich text tag string for proper closing.
        /// </summary>
        /// <param name="tag">The full tag string (e.g., "&lt;color=red&gt;").</param>
        /// <returns>The tag name (e.g., "color").</returns>
        private string ExtractTagName(string tag) {
            string cleanTag = tag.Replace("<", "").Replace(">", "");
            int spaceIndex = cleanTag.IndexOf(' ');
            if (spaceIndex > 0) {
                cleanTag = cleanTag.Substring(0, spaceIndex);
            }
            return cleanTag;
        }

        /// <summary>
        /// Starts the caret blinking animation during typing.
        /// </summary>
        private void StartCaretBlink() {
            if (caretCoroutine != null) {
                StopCoroutine(caretCoroutine);
            }
            caretCoroutine = StartCoroutine(BlinkCaret());
        }

        /// <summary>
        /// Coroutine that handles caret blinking during the typing animation.
        /// Uses invisible characters to prevent text layout shifts when centered.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator BlinkCaret() {
            bool caretVisible = true;

            while (isTyping) {
                yield return new WaitForSeconds(caretBlinkRate);
                caretVisible = !caretVisible;

                if (isTyping && showCaret) {
                    string currentText = textComponent.text;
                    bool hasVisibleCaret = currentText.EndsWith(caretChar.ToString());
                    bool hasInvisibleCaret = currentText.Contains("<color=#00000000>" + caretChar);

                    if (hasVisibleCaret) {
                        currentText = currentText.Substring(0, currentText.Length - 1);
                    }
                    else if (hasInvisibleCaret) {
                        currentText = currentText.Replace("<color=#00000000>" + caretChar + "</color>", "");
                    }

                    if (caretVisible) {
                        currentText += caretChar;
                    }
                    else {
                        currentText += "<color=#00000000>" + caretChar + "</color>";
                    }

                    textComponent.text = currentText;
                }
            }
        }

        /// <summary>
        /// Coroutine that handles caret blinking after typing is complete.
        /// Only runs if keepCaretAfterTyping is enabled.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator BlinkCaretAfterTyping() {
            bool caretVisible = true;
            string baseText = GetTextWithRichTags(processedText.Length);

            while (keepCaretAfterTyping) {
                yield return new WaitForSeconds(caretBlinkRate);
                caretVisible = !caretVisible;

                if (caretVisible) {
                    textComponent.text = baseText + caretChar;
                }
                else {
                    textComponent.text = baseText + "<color=#00000000>" + caretChar + "</color>";
                }
            }
        }

        #endregion
    } 
}