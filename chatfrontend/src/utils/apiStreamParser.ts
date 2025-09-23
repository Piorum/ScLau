export interface StreamedData {
  response: string | null;
  channel: string | null;
}

export class ApiStreamParser {
  private readonly data: any;

  constructor(jsonString: string) {
    try {
      this.data = JSON.parse(jsonString);
    } catch (e) {
      this.data = {};
      console.error('Error parsing JSON:', e, 'Line:', jsonString);
    }
  }

  public getData(): StreamedData {
    const response = this.getString('response');
    const channel = this.getString('channel')?.toLowerCase() ?? null;

    return { response, channel };
  }

  private getString(key: string): string | null {
    if (this.data && typeof this.data[key] === 'string') {
      return this.data[key];
    }
    if (this.data && typeof this.data[key] === 'number') {
        return String(this.data[key]);
    }
    return null;
  }
}
