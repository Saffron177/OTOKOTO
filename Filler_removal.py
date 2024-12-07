import spacy
import sys
import spacy.util
import os
# コマンドライン引数から transcribed_text を受け取る
if len(sys.argv) > 1:
    transcribed_text = sys.argv[1]
else:
    transcribed_text = ""


nlp = spacy.load('ja_ginza')

# モデルのパスを取得
#model_package_path = spacy.util.get_package_path("ja_ginza")
#print(f"ロードされたモデルのパッケージパス: {model_package_path}")


doc = nlp(transcribed_text)
# フィラーの削除
result = ''
for sent in doc.sents:
  for token in sent:
    if token.tag_ != "感動詞-フィラー":
      result += str(token.text)
print(result)