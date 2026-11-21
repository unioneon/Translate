"""Local JSON-lines translation worker. This process never accesses the network."""
import json
import os
from pathlib import Path
import re
import sys
import warnings

warnings.filterwarnings("ignore", category=UserWarning)
os.environ.setdefault("CT2_VERBOSE", "0")
import ctranslate2
import sentencepiece as spm

sys.stdin.reconfigure(encoding="utf-8")
sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)
models_dir = Path(sys.argv[1]).resolve()
loaded = {}


def load_pair(source, target):
    key = source + "-" + target
    if key in loaded:
        return loaded[key]
    pair_root = models_dir / key
    metadata_files = list(pair_root.glob("**/metadata.json"))
    if not metadata_files:
        raise ValueError("MODEL_MISSING")
    root = metadata_files[0].parent
    metadata = json.loads(metadata_files[0].read_text(encoding="utf-8-sig"))
    if metadata.get("from_code") != source or metadata.get("to_code") != target:
        raise ValueError("MODEL_INVALID")
    tokenizer = spm.SentencePieceProcessor(model_file=str(root / "sentencepiece.model"))
    translator = ctranslate2.Translator(str(root / "model"), device="cpu", compute_type="int8", intra_threads=min(4, os.cpu_count() or 2), inter_threads=1)
    loaded[key] = translator, tokenizer, metadata.get("target_prefix", "")
    return loaded[key]


def translate(text, source, target):
    if source == "auto":
        source = "ru" if re.search("[а-яё]", text, re.I) else "en"
    if source not in ("ru", "en") or target not in ("ru", "en"):
        raise ValueError("PAIR_UNSUPPORTED")
    if source == target:
        return text
    translator, tokenizer, prefix = load_pair(source, target)
    paragraphs = []
    for paragraph in text.split("\n"):
        if not paragraph.strip():
            paragraphs.append("")
            continue
        # Keep sentence boundaries and all input tokens; long clauses are batched,
        # never silently truncated to the model's input-length default.
        sentences = re.split(r"(?<=[.!?。！？])\s+", paragraph.strip())
        batches = []
        for sentence in sentences:
            tokens = tokenizer.encode(sentence, out_type=str)
            batches.extend(tokens[i:i + 220] for i in range(0, len(tokens), 220))
        options = dict(beam_size=4, replace_unknowns=True, max_input_length=0, max_decoding_length=512, length_penalty=0.2)
        if prefix:
            options["target_prefix"] = [[prefix]] * len(batches)
        result = translator.translate_batch(batches, **options)
        parts = []
        for item in result:
            value = tokenizer.decode(item.hypotheses[0])
            if prefix and value.startswith(prefix):
                value = value[len(prefix):]
            parts.append(value.strip())
        paragraphs.append(" ".join(parts))
    return "\n".join(paragraphs)


for line in sys.stdin:
    try:
        request = json.loads(line)
        value = translate(request["text"], request["source"], request["target"])
        print(json.dumps({"ok": True, "text": value}, ensure_ascii=False))
    except Exception as exc:
        print(json.dumps({"ok": False, "error": str(exc)}, ensure_ascii=False))
