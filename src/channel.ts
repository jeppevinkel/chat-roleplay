import {Client, Message, Snowflake, TextChannel} from 'discord.js';
import IMessage from './interfaces/iMessage.js';
import CoreConfig from './config/coreConfig.js';
import AiManager from './aiManager.js';
import Character from './character.js';

class Channel {
    private client: Client;
    private channelId: Snowflake;
    private prompt: IMessage[];
    private aiManager: AiManager;
    private characters: Map<string, Character>;
    private coreConfig: CoreConfig;

    private idleTimeout: NodeJS.Timeout | undefined;

    constructor(client: Client, channelId: Snowflake, prompt: IMessage[], aiManager: AiManager, characters: Map<string, Character>, coreConfig: CoreConfig) {
        this.client = client;
        this.channelId = channelId;
        this.prompt = prompt;
        this.aiManager = aiManager;
        this.characters = characters;
        this.coreConfig = coreConfig;

        this.continueIdle().catch(console.error);
    }

    public async handleMessage(message: Message) {
        this.prompt.push({
            role: 'user',
            content: `[NAME] ${message.author.displayName} [MSG] ${message.content}`,
        });

        const response = await this.aiManager.getCompletion(this.prompt);

        console.log('[MESSAGE]', `[NAME] ${message.author.displayName} [MSG] ${message.content}`);
        console.log('[RESPONSE]', response);

        const {char, content} = this.getContent(response!);

        if (content == undefined) {
            console.error('[ON_MESSAGE] No content extracted from AI response.');
        }

        this.prompt.push({
            role: 'assistant',
            content: response!
        });

        if (this.characters.has(char ?? '')) {
            const character = this.characters.get(char ?? '')!;

            await character.reply(message, content!);
        } else {
            await message.reply(content!);
        }
        clearTimeout(this.idleTimeout);
        this.idleTimeout = setTimeout(this.continueIdle.bind(this), this.coreConfig.get('idleIntervalSec') * 1000);
    }

    private async continueIdle() {
        this.prompt.push({
            role: 'user',
            content: 'continue'
        });

        const response = await this.aiManager.getCompletion(this.prompt);
        console.log('[RESPONSE]', response);

        const {char, content} = this.getContent(response!);

        if (content == undefined) {
            console.error('[ON_MESSAGE] No content extracted from AI response.');
        }

        this.prompt.push({
            role: 'assistant',
            content: response!
        });

        const channel = await this.client.channels.fetch(this.channelId) as TextChannel;

        if (this.characters.has(char ?? '')) {
            const character = this.characters.get(char ?? '')!;

            await character.send(channel!, content!);
        } else {
            await channel.send(content!);
        }
        this.idleTimeout = setTimeout(this.continueIdle.bind(this), this.coreConfig.get('idleIntervalSec') * 1000);
    }

    private getContent(message: string) {
        let char = undefined;
        let content = undefined;
        const charMatch = message.match('\\[CHAR\\](.*)\\[CONTENT\\]');
        const contentMatch = message.match('\\[CONTENT\\](.*)');
        if (charMatch) {
            char = charMatch[1].trim();
        }
        if (contentMatch) {
            content = contentMatch[1].trim();
        }

        return {content, char};
    }
}

export default Channel;