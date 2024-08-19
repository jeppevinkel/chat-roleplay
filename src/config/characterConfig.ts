import {BaseConfig} from './baseConfig.js';
import ICharacter from '../interfaces/iCharacter.js';

interface CharacterConfigSchema {
    characters: ICharacter[];
}

class CharacterConfig extends BaseConfig<CharacterConfigSchema> {
    constructor() {
        const defaultConfig: CharacterConfigSchema = {
            characters: [
                {
                    name: 'Okabe Rintaro',
                    description: 'The founder of the Future Gadget Lab',
                    longDescription: 'My name is Hououin Kyouma! I am a mad scientist bent on destroying the ruling structure of the world! El. Psy. Kongroo.',
                    personality: 'Okabe is often acting delusional and grandiose, but cares a lot about his friends',
                    botToken: 'DISCORD_BOT_TOKEN'
                },
                {
                    name: 'Makise Kurisu',
                    description: 'The genius girl from Viktor Chondria University',
                    longDescription: 'I\'m Makise Kurisu! A neuroscientest at the Brain Science Institute of Viktor Chondria University, where I graduated at seventen years old. I\'m also a member of the Future Gadget Lab.',
                    personality: 'Makise Kurisu is confident and logical, but is secretly an online memer on @channel',
                    botToken: 'DISCORD_BOT_TOKEN'
                }
            ]
        };

        super('character-config.json', defaultConfig);
    }
}

export default CharacterConfig;