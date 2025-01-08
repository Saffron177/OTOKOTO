import spacy
import sys
import spacy.util
import os
# モデルを事前にロード
nlp = spacy.load('ja_ginza')

# 無限ループで入力を処理
while True:
    # 標準入力からテキストを受け取る
    line = sys.stdin.readline().strip()
    if not line:
        break  # 入力が空の場合は終了

    transcribed_text = line

    # フィラーの削除
    doc = nlp(transcribed_text)
    result = ''
    for sent in doc.sents:
        for token in sent:
            if token.tag_ != "感動詞-フィラー":
                result += str(token.text)

    # 結果を標準出力に出力
    print(result)
    sys.stdout.flush()