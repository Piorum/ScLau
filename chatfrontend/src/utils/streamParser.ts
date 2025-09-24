class ApiStreamParser {
  private readonly data: any;

  constructor(jsonString: string) {
    try {
      this.data = JSON.parse(jsonString);
    } catch (e) {
      this.data = {};
    }
  }

  public getData(): { response: string | null; channel: string | null } {
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

export type MessageStreamEvent = 
  | { type: 'reasoning_started'; payload: { id: string; text: string } }
  | { type: 'reasoning_append'; payload: { id: string; text: string } }
  | { type: 'answer_started'; payload: { id: string; text: string } }
  | { type: 'answer_append'; payload: { id: string; text: string } }
  | { type: 'stream_done' };

export async function* streamChat(reader: ReadableStreamDefaultReader<Uint8Array>): AsyncGenerator<MessageStreamEvent> {
  const decoder = new TextDecoder();
  let buffer = '';
  let reasoningId: string | null = null;
  let answerId: string | null = null;

  while (true) {
    const { done, value } = await reader.read();

    if (done) {
      if (buffer) {
        // Process any remaining data in the buffer
        const parsed = new ApiStreamParser(buffer).getData();
        if (parsed.response) {
          if (answerId) {
            yield { type: 'answer_append', payload: { id: answerId, text: parsed.response } };
          } else if (reasoningId) {
            yield { type: 'reasoning_append', payload: { id: reasoningId, text: parsed.response } };
          }
        }
      }
      yield { type: 'stream_done' };
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (!line) continue;
      const { response, channel } = new ApiStreamParser(line).getData();

      if (response && channel) {
        const isReasoning = channel !== 'final';

        if (isReasoning) {
          if (!reasoningId) {
            reasoningId = (Date.now() + Math.random()).toString();
            yield { type: 'reasoning_started', payload: { id: reasoningId, text: response } };
          } else {
            yield { type: 'reasoning_append', payload: { id: reasoningId, text: response } };
          }
        } else { // channel is 'final'
          if (!answerId) {
            answerId = (Date.now() + Math.random()).toString();
            yield { type: 'answer_started', payload: { id: answerId, text: response } };
          } else {
            yield { type: 'answer_append', payload: { id: answerId, text: response } };
          }
        }
      }
    }
  }
}