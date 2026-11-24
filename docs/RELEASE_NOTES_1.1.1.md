# Translate 1.1.1

This patch handles temporary Google API failures more clearly.

- Translation requests retry HTTP 500, 502, 503 and 504 up to twice, with increasing delays and jitter.
- Short Retry-After instructions are respected. Long outages return control to the user.
- The total translation timeout stays at 45 seconds.
- Persistent server failures show an availability message instead of suggesting incorrect language or API settings.
- Key, quota and validation errors are not retried.

Transport behavior was tested with simulated HTTP responses, including recovery after 503, repeated failures, unchanged request bodies and cancellation. An active-key live translation was not part of this validation. Retries cannot guarantee availability during a Google outage.

Download Setup to install or extract the complete Portable ZIP. Windows 10/11 x64, .NET Framework 4.8 and WebView2 are required. Executables are unsigned. No API key is bundled.
