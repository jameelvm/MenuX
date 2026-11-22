# Library choices and licences: image processing and QR codes (L6)

Research only, 2026-11-21 (doc 14 §6, week 1). **Nothing is installed or decided.** The image library is needed in week 3 (uploads + WebP renditions, doc 10 §12) and the QR library in the publishing/QR work. Doc 12 R-7 already flagged ImageSharp's split licence and suggested an MIT alternative such as SkiaSharp. Versions and licences come from NuGet package metadata on this date.

Tags: [S] stated · [D] derived · [A] assumption · [R] recommendation.

## 1. Image processing (WebP renditions)

Needs [D] (doc 09 media: magic-byte validation, EXIF stripping, WebP renditions; doc 07 MD4; doc 12: WebP only, two rendition sizes, no AVIF in MVP-1): decode common upload formats (JPEG/PNG/WebP); resize; strip metadata; encode WebP; run on Linux containers (ECS Fargate) and in CI; bounded memory for large uploads.

| Library | Version (published) | Licence | WebP | Native dependency | Notes |
|---------|---------------------|---------|------|-------------------|-------|
| **SixLabors.ImageSharp** | 4.1.3 (2026-10-09) | **Six Labors Split License 1.0**: Apache-2.0 only if the software is open source, the library is a transitive dependency, the user is a non-profit, or the user is a for-profit company with **under USD 1M annual gross revenue**; otherwise a **paid commercial licence** | Yes | None (fully managed) | Simplest to deploy. The licence becomes a cost once the business grows past the revenue threshold. No HEIC. |
| **SkiaSharp** | 4.157.0 (2026-11-18) | MIT (Skia: BSD-3-Clause) | Yes (encode and decode) | Native Skia; Linux needs `SkiaSharp.NativeAssets.Linux` (or the `.NoDependencies` variant) | Permissive, actively maintained, net10.0 target. No HEIC. |
| **NetVips** + NetVips.Native | 3.2.0 (2026-01-01) / 8.18.7 (2026-09-28) | NetVips: MIT; **libvips native binaries: LGPL-3.0-or-later** | Yes | Bundled native libvips | Fastest and lowest memory (streaming). LGPL dynamic linking is usually acceptable but needs a licence notice and the ability to swap the library; get legal confirmation. HEIC support depends on the build. |
| **Magick.NET** (Q8) | 15.1.0 (2026-11-05) | Apache-2.0 (ImageMagick licence) | Yes | Bundled native ImageMagick | Widest format support, including HEIC. Larger package, higher memory use, and ImageMagick's history of security issues means untrusted input needs a strict policy (allowed formats, resource limits). |

**[R] Recommendation:** **SkiaSharp**. It's permissive (MIT), maintained, has WebP encode and decode, and has no revenue-based licence. Trial it in the media week on the Linux container image, checking memory on a large phone photo at the maximum upload size (not yet set). Use NetVips if memory or throughput falls short (after a legal check of LGPL-3.0). Avoid ImageSharp unless a commercial licence is budgeted. HEIC isn't mentioned in the requirements [A: not needed in MVP-1]; if it becomes one, Magick.NET with a locked-down policy is the option.

## 2. QR code generation

Needs [D] (doc 14 MVP-1 scope: QR codes, 128-bit, PNG/SVG, PDF is flex; doc 04 §4.2): generate a QR code for a URL (`/q/{128-bit base64url}`); SVG and PNG output; error-correction level selectable; no network calls.

| Library | Version (published) | Licence | Output | Notes |
|---------|---------------------|---------|--------|-------|
| **QRCoder** | 1.9.0 (2026-11-19) | MIT | SVG, PNG (without System.Drawing), others | Most widely used .NET QR library; actively maintained |
| **Net.Codecrete.QrCodeGenerator** | 3.2.1 (2026-09-07) | MIT | SVG and PNG via a small helper; a port of Nayuki's reference implementation | Small and correct; fewer output helpers |
| **ZXing.Net** | 0.16.11 (2025-10-25) | Apache-2.0 | Many barcode formats; rendering through binding packages | Broader than needed; mainly useful for decoding. [R] Use for **tests** (decode the generated QR and compare the URL), not generation. |

**[R] Recommendation:** **QRCoder** (MIT) for generation, with a test that decodes the output (for example with ZXing.Net in the test project only) and checks the exact URL.

## 3. Licence follow-ups (L6)

- Keep a third-party notices file when the first runtime package is added.
- NetVips (LGPL-3.0 native) or ImageSharp (split licence) would need a legal or commercial check before adoption.
- Re-check versions and licences when each library is actually added.
