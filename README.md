# Builder
Bloxstrap build tool made specifically for translators, allowing for easy QA of translations by being able to test them in context on-demand.

## Using Builder
When using it for the first time, there are two things you need to:
- [Configure Crowdin](#configuring-crowdin)
- Set a language. Enter `list languages` to see all available languages, then set one by entering `set language <langname>`.

Afterwards, it's as simple as entering `build` to build, and `run` to run your latest build.

When upgrading Builder, be sure to preserve config.json.

## QA Builds

Builder produces a special QA build of Bloxstrap. It is completely separate from your standard Bloxstrap installation, and makes it easier to check coverage and freely test things.

Remember that Bloxstrap takes over as the web launch handler whenever the bootstrapper is ran. If the QA build is registered as the web launch handler and you want to return it back to standard Bloxstrap, simply launch Roblox from standard Bloxstrap once.

## Configuring Crowdin

A Crowdin API token is required to fetch translations. You will need to configure Builder to use one for your own account.

To create one, go to https://crowdin.com/settings#api-key. You must grant read/write access for the "Projects" scope.

![image](https://github.com/user-attachments/assets/45dc85a1-0411-48c1-aba4-52033c91310c)

Copy the token it gives you, enter `configure crowdin` in Builder, and paste in the token.

![image](https://github.com/user-attachments/assets/9943e549-ba24-4e1b-b05b-1e56540b46e7)
