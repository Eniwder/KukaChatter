import argparse
from pathlib import Path
from urllib.parse import unquote

import torch
from scipy.io import wavfile

from config import get_config
from style_bert_vits2.constants import (
    DEFAULT_LINE_SPLIT,
    DEFAULT_NOISE,
    DEFAULT_NOISEW,
    DEFAULT_SDP_RATIO,
    DEFAULT_SPLIT_INTERVAL,
    DEFAULT_STYLE,
    DEFAULT_STYLE_WEIGHT,
    Languages,
)

from style_bert_vits2.nlp import bert_models
from style_bert_vits2.nlp.japanese import pyopenjtalk_worker as pyopenjtalk
from style_bert_vits2.nlp.japanese.user_dict import update_dict
from style_bert_vits2.tts_model import TTSModel, TTSModelHolder


config = get_config()
# ln = config.server_config.language

# pyopenjtalk_worker を起動
## pyopenjtalk_worker は TCP ソケットサーバーのため、ここで起動する
pyopenjtalk.initialize_worker()

# dict_data/ 以下の辞書データを pyopenjtalk に適用
update_dict()

bert_models.load_model(Languages.JP)
bert_models.load_tokenizer(Languages.JP)

loaded_models: list[TTSModel] = []

def load_models(model_holder: TTSModelHolder):
    global loaded_models
    loaded_models = []
    for model_name, model_paths in model_holder.model_files_dict.items():
        model = TTSModel(
            model_path=model_paths[0],
            config_path=model_holder.root_dir / model_name / "config.json",
            style_vec_path=model_holder.root_dir / model_name / "style_vectors.npy",
            device=model_holder.device,
        )
        # 起動時に全てのモデルを読み込むのは時間がかかりメモリを食うのでやめる
        # model.load()
        loaded_models.append(model)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--cpu", action="store_true", help="Use CPU instead of GPU")
    parser.add_argument("--dir", "-d", type=str, help="Model directory", default=config.assets_root)
    parser.add_argument("--text", "-t", type=str, help="Voice text", default="デフォルトテキスト")
    parser.add_argument("--outname", "-f", type=str, help="Output file name", default="output")
    args = parser.parse_args()

    if args.cpu:
        device = "cpu"
    else:
        device = "cuda" if torch.cuda.is_available() else "cpu"

    text = args.text
    outname = args.outname
    model_dir = Path(args.dir)
    model_holder = TTSModelHolder(model_dir, device)

    load_models(model_holder)

    def voice(
        text: str = text,
        encoding: str = "utf-8",
        model_name: str = "juewa",
        model_id: int = 0,
        speaker_id: int =0,
        sdp_ratio: float =  DEFAULT_SDP_RATIO,
        noise:float= DEFAULT_NOISE,
        noisew:float=DEFAULT_NOISEW,
        length:float= 1.1, # DEFAULT_LENGTH
        language:Languages = Languages.JP,
        auto_split:bool= DEFAULT_LINE_SPLIT,
        split_interval:float=DEFAULT_SPLIT_INTERVAL,
        style: str = DEFAULT_STYLE,
        style_weight:float=DEFAULT_STYLE_WEIGHT,
    ):
        """Infer text to speech(テキストから感情付き音声を生成する)"""
        if model_name:
            # load_models() の 処理内容が i の正当性を担保していることに注意
            model_ids = [i for i, x in enumerate(model_holder.models_info) if x.name == model_name]
            model_id = model_ids[0]
            
        model = loaded_models[model_id]

        if encoding is not None:
            text = unquote(text, encoding=encoding)

        sr, audio = model.infer(
            text=text,
            language=language,
            speaker_id=speaker_id,
            sdp_ratio=sdp_ratio,
            noise=noise,
            noise_w=noisew,
            length=length,
            line_split=auto_split,
            split_interval=split_interval,
            style=style,
            style_weight=style_weight,
        )

        with open(f'./result/{outname}.wav', 'wb') as wav_file:
            wavfile.write(wav_file, sr, audio)

    voice()