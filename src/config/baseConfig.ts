import {readFile, writeFile, mkdir} from 'fs/promises';
import {existsSync} from 'fs';
import {join} from 'path';
import {Base} from 'discord.js';

interface ConfigSchema {
    [key: string]: any;
}

export abstract class BaseConfig<T extends ConfigSchema> {
    private configData: T;
    private configFilePath: string;

    constructor(configFileName: string, defaultConfig: T) {
        // Define the path to the config file
        const dataDirectory = join('.', 'data');
        this.configFilePath = join(dataDirectory, configFileName);

        // Initialize default config data
        this.configData = {...defaultConfig};

        // Ensure the data directory exists
        if (!existsSync(dataDirectory)) {
            // create directory if it doesn't exist
            mkdir(dataDirectory, {recursive: true}).catch(console.error);
        }
    }

    public async load(): Promise<void> {
        try {
            if (existsSync(this.configFilePath)) {
                const data = await readFile(this.configFilePath, 'utf-8');
                const parsedData = JSON.parse(data);
                this.configData = {...this.configData, ...parsedData};
            }
            await this.save();
        } catch (error) {
            console.error('Error loading config:', error);
        }
    }

    public async save(): Promise<void> {
        try {
            const data = JSON.stringify(this.configData, null, 2);
            await writeFile(this.configFilePath, data, 'utf-8');
        } catch (error) {
            console.log('Error saving config:', error);
        }
    }

    public get<K extends keyof T>(key: K): T[K] {
        return this.configData[key];
    }

    public set<K extends keyof T>(key: K, value: T[K]) {
        this.configData[key] = value;
    }
}