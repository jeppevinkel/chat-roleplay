import axios, {AxiosInstance} from 'axios';
import IMessage from './interfaces/iMessage.js';
import {SupportedModels, SupportedProviders} from './interfaces/literalTypes.js';

class AiManager {
    private axios: AxiosInstance;
    private model: SupportedModels;
    private provider: SupportedProviders;

    constructor(provider: SupportedProviders, customEndpoint: string | undefined, model: SupportedModels, token: string) {
        switch (provider) {
            case 'openai':
                this.axios = axios.create({
                    baseURL: customEndpoint ? customEndpoint : 'https://api.openai.com',
                    headers: {
                        Authorization: `Bearer ${token}`,
                    }
                });
                break;
            case 'ollama':
                this.axios = axios.create({
                    baseURL: customEndpoint ? customEndpoint : 'http://localhost:11434'
                });
                break;
            default:
                throw new Error('Unsupported provider');
        }

        this.model = model;
        this.provider = provider;
    }

    public async getCompletion(messages: IMessage[]): Promise<string | undefined> {
        try {
            switch (this.provider) {
                case 'openai': {
                    const response = await this.axios.post('/v1/chat/completions', {
                        model: this.model,
                        messages: messages,
                    });

                    return response.data.choices[0].message.content as string;
                }
                case 'ollama': {
                    const response = await this.axios.post('/api/chat', {
                        model: this.model,
                        messages: messages,
                        stream: false
                    });

                    return response.data.message.content as string;
                }
            }

        } catch (error) {
            console.error(error);
        }
        return undefined;
    }
}

export default AiManager;