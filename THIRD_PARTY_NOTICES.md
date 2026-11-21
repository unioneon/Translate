# Third-party notices

Translate's own source code and artwork are MIT licensed. Third-party components retain their respective licenses.

| Component | Version | License / source |
|---|---|---|
| Microsoft WebView2 SDK | 1.0.3296.44 | Microsoft BSD-style license; see `licenses/WebView2-LICENSE.txt` and `licenses/WebView2-NOTICE.txt` in each distribution |
| Microsoft Edge WebView2 Runtime | Evergreen, installed separately | Microsoft's runtime terms; the signed Microsoft bootstrapper is included with Setup |
| Python | 3.12.10 | PSF License; included in `offline-runtime/LICENSE.txt` |
| CTranslate2 | 4.6.0 | MIT; `licenses/CTranslate2-LICENSE.txt` |
| SentencePiece | 0.2.1 | Apache-2.0; `licenses/SentencePiece-LICENSE.txt` |
| NumPy | 2.2.6 | BSD and bundled dependency notices in `offline-runtime/Lib/site-packages/numpy-2.2.6.dist-info/LICENSE.txt` |
| PyYAML | 6.0.2 | MIT; its bundled `.dist-info/LICENSE` |
| setuptools | 80.9.0 | MIT and vendored-component licenses included with the runtime |

Offline model weights are **not** included in the installer, portable package, or source repository. The app downloads Russian↔English packages from the public [Argos model index](https://github.com/argosopentech/argospm-index), preserving each model's metadata and README notices. Model and training-data terms are separate from Translate's MIT license.

Google Gemini is an external service governed by Google's terms. Translate does not include an API key or a Google subscription. No Google, Microsoft, or Argos affiliation is implied.

The screenshots show Translate's own interface and use an original abstract background. No Windows wallpaper or stock photography is redistributed.
