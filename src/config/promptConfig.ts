import {BaseConfig} from './baseConfig.js';
import ICharacter from '../interfaces/iCharacter.js';
import IMessage from '../interfaces/iMessage.js';

interface PromptConfigSchema {
    template: IMessage[];
}

class PromptConfig extends BaseConfig<PromptConfigSchema> {
    constructor() {
        const defaultConfig: PromptConfigSchema = {
            template: [
                {
                    role: 'system',
                    content: 'You will respond as one of {num_chars} characters: {chars}. Always respond using a [CHAR] response, which includes only the name of one character at a time, followed by [CONTENT] where the character speaks. The format is: [CHAR] CharacterName [CONTENT] What the character says. For example: [CHAR] Monika [CONTENT] Hello, everyone! Each character has a distinct personality: {chars_personality}. If I use [NAME] and [MSG], you should respond as if the character is speaking to the person named in [NAME], but always use the [CHAR] [CONTENT] format in your response. If the [NAME] is unusual, like \\"<@1234567890>\\", refer to them exactly as typed. When I type \'{RST}\', reset the story and start fresh with a [CHAR] response. Only one character should speak at a time, and you should never include more than one [CHAR] response in a single output. If you understand, respond with a [CHAR] type response.'
                },
                {
                    role: 'user',
                    content: 'continue'
                },
                {
                    role: 'assistant',
                    content: '[CHAR] Monika [CONTENT] Hello everyone! Welcome to the Literature Club! I\'m Monika, the president. I\'m so glad you could join us today. We\'re going to have a wonderful time discussing literature and getting to know each other. Sayori, would you like to start by introducing yourself?'
                },
                {
                    role: 'user',
                    content: '{RST}'
                }
            ],
        };

        super('prompt-config.json', defaultConfig);
    }

    public getFormattedTemplate(characters: ICharacter[]) {
        let template = structuredClone(this.get('template'));
        let content = template[0].content;

        content = content.replace('{num_chars}', characters.length.toString());
        content = content.replace('{chars}', characters.map(it => it.name).join(', '));
        content = content.replace('{chars_personality}', characters.map(it => it.personality).join('. '));

        template[0].content = content;

        return template;
    }
}

export default PromptConfig;