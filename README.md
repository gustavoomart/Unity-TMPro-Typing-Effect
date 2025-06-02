# Unity TMPro Typing Effect
A typing effect component for TextMeshPro that simulates typewriter-style text animation. Supports rich text formatting, customizable timing, optional blinking caret, and asynchronous playback.

## Rich Text
Natively supports rich text, just pass the message parameter with the tags, example using full methos async with custom time and multiple messages:
``` csharp
typingEffect.PlayTextsSequentially(
	2f,
	"Hello <color=red>World</color>!",
	"Hello <color=yellow>Again</color>!"
);
```

## API


### `IsTyping`

 Gets whether the typing effect is currently active.

**Returns:**
- `True` if text is currently being typed, `false` otherwise.

```csharp
public bool IsTyping() => isTyping;
```

### `PlayText`

Starts typing the specified message with the configured settings. This is a fire-and-forget method that doesn't wait for completion.

**Parameters:**
- `message`: The text to type, supports TextMeshPro rich text formatting.

```csharp
TypingEffect typingEffect;
private void PlaySomeText() {
    typingEffect.PlayText("Hello World!");
}
```

### `PlayTextAsync`

Asynchronously types the specified message and waits for completion. Use this method when you need to wait for the typing to finish before continuing.

**Parameters:**
- `message`: The text to type, supports TextMeshPro rich text formatting.

**Returns:**
- A task that completes when the typing animation finishes.

```csharp
[SerializeField] TypingEffect typingEffect;
private void Start() {
	StartCoroutine(PlaySomeText());
}

private IEnumerator PlaySomeText() {
	var task = typingEffect.PlayTextAsync("Hello World!");
	yield return new WaitUntil(() => task.IsCompleted);
}
```

### `StopTyping`

Immediately stops the current typing animation and displays the complete text.


### `WaitForCurrentText`

Waits for the current typing animation to complete if one is active.

**Returns:**
- A task that completes when the current typing finishes.

### `PlayTextsSequentially` (with default pause)

Plays multiple text messages in sequence, waiting for each to complete before starting the next. Uses the default caret blink rate as pause duration between messages.

**Parameters:**
- `messages`: Array of text messages to play sequentially.

**Returns:**
- A task that completes when all messages have been typed.

```csharp
[SerializeField] TypingEffect typingEffect;
private void Start() {
    StartCoroutine(PlayIntroText());
}

private IEnumerator PlayIntroText() {
    var task = typingEffect.PlayTextsSequentially("Hello World!", "Hello Again!");
    yield return new WaitUntil(() => task.IsCompleted);
}
```

### `PlayTextsSequentially` (with custom pause)

Plays multiple text messages in sequence with custom pause duration between messages.

**Parameters:**
- `pauseBetweenTexts`: Time to wait between each message in seconds.
- `messages`: Array of text messages to play sequentially.

**Returns:**
- A task that completes when all messages have been typed.

```csharp
[SerializeField] TypingEffect typingEffect;
private void Start() {
    StartCoroutine(PlayIntroText());
}

private IEnumerator PlayIntroText() {
    var task = typingEffect.PlayTextsSequentially(2f, "Hello World!", "Hello Again!");
    yield return new WaitUntil(() => task.IsCompleted);
}
```

### `SetTotalTypingTime`

Sets the total time for typing animation. The text will always complete within this duration.

**Parameters:**
- `time`: Total typing time in seconds (minimum 0.1 seconds).



### `SetNoiseVariation`

Sets the random variation applied to individual character timing.

**Parameters:**
- `noise`: Noise variation amount (0-1 range, where 0 = no variation, 1 = maximum variation).


### `SetCaretChar`

Sets the character used for the blinking caret cursor.

**Parameters:**
- `caret`: The character to use as caret cursor.


### `SetCaretBlinkRate`

Sets how fast the caret blinks.

**Parameters:**
- `rate`: Blink rate in seconds (minimum 0.1 seconds).


### `SetShowCaret`

Enables or disables the caret cursor entirely.

**Parameters:**
- `show`: True to show caret, false to hide it completely.


### `SetKeepCaretBlinking`

Sets whether the caret should continue blinking after typing is complete.

**Parameters:**
- `keep`: True to keep caret blinking after typing, false to hide it.
