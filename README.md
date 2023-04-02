# Aikashka

Talk with GPT 3.5 model right in Discord. Powered by OpenAI's Whisper & GPT.

## How it works

Well, your voice is sent to the local Whisper backend to get text which adds to the room context.

Aikashka will respond every 10-40 seconds or by calling her(/him) by name.

When the time comes, a big prompt is being sent to the OpenAI's GPT endpoint. The answer will be strictly formatted.
Depending on the response, Aikashka can:
- say something
- write something in the text channel
- kick someone from the voice channel
- save some note for herself
- leave the channel

Note that she remembers only 20 last user messages and 6 her own responses, all in order.

## Setup

### Notes

I don't do pre-made binary. It means that you have to clone this repo, open it in JetBrains Rider / Fleet (or Visual Studio (/Code) if you don't respect yourself) and make some changes on your own.

TTS made using Tinkoff VoiceKit (well, it's really cheap and the voice is REALLY good).

### How To

#### Required

Rename `appsettings.example.json` to `appsettings.json` and fill all required API keys.

Rename `name_mappings.example.txt` to `name_mappings.txt` and map all user profiles that will talk with the bot.

#### Lore

Take a look at `systemMsg` in `Aikashka.Core/Generators/TextGenerator.cs` - first 3 lines. Change them if you want.

#### Whisper backend (`aikashka-backend/`)

Create virtual environment & install `requirements.txt`.

If you want GPU acceleration, install PyTorch with CUDA, e.g. `pip install torch --index-url https://download.pytorch.org/whl/cu117`

*And if you don't, then remove `, device=0` from core.py*

Also ensure that model fits your needs.

#### Run

Compile & run bot.

Invite bot to the server and use `/go` command to join the voice channel.

## Commands

- `/go`: forces bot to join the channel.
- `/cum`: send message to the room context. useful if you can't spell properly some words
- `/state`: shows room context.
- `/clear`: clear room context. 
