import {Client, Events, GatewayIntentBits, Snowflake} from 'discord.js';
import CharacterConfig from './config/characterConfig.js';
import CoreConfig from './config/coreConfig.js';
import Character from './character.js';
import PromptConfig from './config/promptConfig.js';
import AiManager from './aiManager.js';
import Channel from './channel.js';

async function run() {
    // Load configs
    const coreConfig = new CoreConfig();
    const promptConfig = new PromptConfig();
    const characterConfig = new CharacterConfig();
    await coreConfig.load();
    await promptConfig.load();
    await characterConfig.load();
    // Loaded configs

    const aiManager = new AiManager(coreConfig.get('provider'), coreConfig.get('customEndpoint'), coreConfig.get('model'), coreConfig.get('aiToken'));

    const characters: Map<string, Character> = new Map();
    const channels: Map<Snowflake, Channel> = new Map();

    for (const character of characterConfig.get('characters')) {
        characters.set(character.name, new Character(character));
    }

    await characterConfig.save();

    const client = new Client({
        intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages, GatewayIntentBits.MessageContent]
    });

    client.once(Events.ClientReady, (client) => {
        console.log(`Logged in as ${client.user.tag}`);

        for (const channel of coreConfig.get('roleplayChannels')) {
            channels.set(channel, new Channel(client, channel, promptConfig.getFormattedTemplate(characterConfig.get('characters')), aiManager, characters, coreConfig));
        }
    });

    client.on(Events.MessageCreate, async (message) => {
        if (message.author.bot) return;

        if (channels.has(message.channelId)) {
            const channel = channels.get(message.channelId);
            await channel?.handleMessage(message);
        }
    });

    await client.login(coreConfig.get('managerBot').botToken);
}

run().catch(console.error);