# discord-wallet-tracker

This is an application designed to monitor EVM compatible wallets, such as the Treasury Gnosis Safe Wallet and report it to Discord via Webhook.

## **Running the app**
Docker is the only supported way to deploy the app.
### Requirements
- [docker](https://docs.docker.com/engine/install/)
- [docker compose](https://docs.docker.com/compose/install/)

### How to run
1. Clone this repository: ``git clone --depth 1 https://github.com/Tangle-Community-Treasury-DAO/discord-wallet-tracker``
2. Create an ``appsettings.json`` file in the bot's root directory, you can find and example in the bot's root directory or here: [appsettings.Example.json](https://raw.githubusercontent.com/Tangle-Community-Treasury-DAO/discord-wallet-tracker/develop/appsettings.Example.json).
3. Configure ``appsettings.json`` to fit your needs.
4. Build and run the bot by issuing ``docker-compose up -d --build`` in the bot's root directory.

### How to stop
1. To stop the bot and issue ``docker-compose down`` in the bot's root directory.

### How to update
1. Update your git repository: ``git pull``
2. Build and run the bot again: ``docker-compose up -d --build``

## **Developing the app**
### Requirements
- Visual Studio Code
- dotnet SDK 7.0

### Working on the project
1. Open the wallet-tracker.code-workspace file with Visual Studio Code
2. Install the recommended plugins
3. Work on it
4. ???
5. Profit

Use ``appsettings.Development.json`` to set development specific overrides.  
**Please help us to make the developer experience great with other IDE's, it is greatly appreciated!**