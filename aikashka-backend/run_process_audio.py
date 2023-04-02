import requests

res = requests.post('http://127.0.0.1:5000/process_audio', files={
    'audio': open('./res.wav', 'rb')
})
print(res.text)
