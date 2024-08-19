import {Channel, Client, Events, GatewayIntentBits, Message} from 'discord.js';
import ICharacter from './interfaces/iCharacter.js';

class Character {
    private readonly client: Client;
    private readonly character: ICharacter;

    constructor(character: ICharacter) {
        this.character = character;
        this.client = new Client({
            intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages]
        });

        this.client.once(Events.ClientReady, (client) => {
            this.log(`Logged in as ${client.user.tag}`);
        });

        this.client.login(character.botToken).catch(error => this.error(`Failed to log ${character.name} in:`, error));
    }

    public async reply(message: Message<boolean>, response: string) {
        let fetchedMessage = undefined;
        let channel = await this.client.channels.fetch(message.channelId);
        if (channel?.isTextBased())
            fetchedMessage = await channel.messages.fetch(message.id);

        await fetchedMessage?.reply(response);
    }

    public async send(channel: Channel, response: string) {
        let fetchedChannel = await this.client.channels.fetch(channel.id);
        if (fetchedChannel?.isTextBased())
            await fetchedChannel.send(response);
    }

    private log(message?: any, ...optionalParams: any[]) {
        console.log(`[${this.character.name}] ${message}`, ...optionalParams);
    }

    private error(message?: any, ...optionalParams: any[]) {
        console.error(`[${this.character.name}] ${message}`, ...optionalParams);
    }
}

export default Character;