from traceback import format_exc
from time import time
from core import *
from flask import Flask, request

app = Flask(__name__)


@app.get("/ping")
def ping():
    return 'Hello from Aikashka!'


@app.post("/process_audio")
def process_audio():
    file = request.files['audio']
    audio_bytes = file.stream.read()

    audio = load_audio(audio_bytes)

    try:
        result = stt_model(audio)['text']
    except:
        print(format_exc())

        with open('./errored/' + str(time()) + '.wav', 'wb') as f:
            f.write(audio_bytes)

        return '[Aikashka] -1'

    print(result)

    tt = result.lower()
    if len(tt) < 8 or len(tt.split()) < 2:
        return '[Aikashka] 0'

    return result


print('run happened')
