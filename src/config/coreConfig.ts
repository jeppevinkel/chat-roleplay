import {BaseConfig} from './baseConfig.js';
import IManagerBot from '../interfaces/iManagerBot.js';
import {SupportedModels, SupportedProviders} from '../interfaces/literalTypes.js';
import {Snowflake} from 'discord.js';

interface CoreConfigSchema {
    managerBot: IManagerBot;
    model: SupportedModels;
    provider: SupportedProviders;
    aiToken: string;
    idleIntervalSec: number;
    roleplayChannels: Snowflake[];
}

class CoreConfig extends BaseConfig<CoreConfigSchema> {
    constructor() {
        const defaultConfig: CoreConfigSchema = {
            managerBot: {
                botToken: ''
            },
            model: 'gpt-4o',
            provider: 'openai',
            aiToken: '',
            idleIntervalSec: 180,
            roleplayChannels: []
        };

        super('core-config.json', defaultConfig);
    }
}

export default CoreConfig;