# Security

Never post API keys, `settings.dat`, your WebView2 profile, or recordings in an issue.

For private security reports, use GitHub's **Report a vulnerability** option when available. Otherwise, open an issue containing only a non-sensitive summary and request a private contact route.

Keys are encrypted with Windows DPAPI for the current Windows user. Online translation sends the submitted text to Google Gemini. Voice input sends the recorded audio after the user stops recording. Offline translation does not send translation text or audio to cloud services; model downloads contact GitHub and the model host.

The keyboard launcher only matches the configured shortcut. It does not store or transmit keystrokes.
