import axios, {AxiosInstance} from 'axios';
import IMessage from './interfaces/iMessage.js';
import {SupportedModels, SupportedProviders} from './interfaces/literalTypes.js';

class AiManager {
    private axios: AxiosInstance;
    private model: SupportedModels;

    constructor(provider: SupportedProviders, model: SupportedModels, token: string) {
        switch (provider) {
            case 'openai':
                this.axios = axios.create({
                    baseURL: 'https://api.openai.com',
                    headers: {
                        Authorization: `Bearer ${token}`,
                    }
                });
                break;
            default:
                throw new Error('Unsupported provider');
        }

        this.model = model;
    }

    public async getCompletion(messages: IMessage[]): Promise<string | undefined> {
        try {
            const response = await this.axios.post('/v1/chat/completions', {
                model: this.model,
                messages: messages
            });

            return response.data.choices[0].message.content as string;
        } catch (error) {
            console.error(error);
        }
        return undefined;
    }
}

export default AiManager;