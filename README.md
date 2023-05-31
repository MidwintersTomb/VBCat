# VBCat
Netcat style listener and remote connection tool written in VB.Net

This started out as a bit of a shitposter style joke when a friend said we should make a C2 together, and I joked we should make it in VB, but since I'm a terrible programmer, see if I can get ChatGPT to write the code, and then do any corrections, tweaks, etc. that I need to to get it work as intended.  This is just the first step, I'm currently also working on a VB.Net multihandler, we'll see if I can get that working correctly.

So, that's exactly what this is.  Through ChatGPT prompts, the bulk of the code was produced, it's terrible, but, really, this was done as a joke anyhow.  Ironically, by having to fix ChatGPT's errors, I've learned more VB than I knew before, so it's also been a bit of a learning experience.

## Usage:
```
Usage: VBCat.exe <mode> [options]

Modes:

-c <hostname> <port>    (Connect to a remote host)

-l <port>               (Listen for incoming connections)
```

### Note:
-c currently works like a Windows run box, any command you enter is passed to cmd /c and returns the result (if you want to use PowerShell, feel free to tweak the source code, it's annotated).

I think it goes without saying, however ***use at your own risk*** I'm not responsible for what you do with it.
