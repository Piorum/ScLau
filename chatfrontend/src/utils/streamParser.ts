export interface ModelResponse {
    MessageId: string;
    ContentType: number; // 0 for Reasoning, 1 for Answer
    ContentChunk: string;
    IsDone: boolean;
}

export type MessageStreamEvent =
  | { type: 'reasoning_started'; payload: { messageId: string; chunk: string } }
  | { type: 'reasoning_append'; payload: { messageId: string; chunk: string } }
  | { type: 'answer_started'; payload: { messageId: string; chunk: string } }
  | { type: 'answer_append'; payload: { messageId: string; chunk: string } }
  | { type: 'stream_done' };

export async function* streamChat(reader: ReadableStreamDefaultReader<Uint8Array>): AsyncGenerator<MessageStreamEvent> {
  const decoder = new TextDecoder();
  let buffer = '';
  let reasoningMessageId: string | null = null;
  let answerMessageId: string | null = null;

  while (true) {
    const { done, value } = await reader.read();

    if (done) {
        yield { type: 'stream_done' };
        break;
    }

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (!line.trim()) continue;

      try {
        const parsed: ModelResponse = JSON.parse(line);
        const { MessageId, ContentType, ContentChunk, IsDone } = parsed;

        if (ContentChunk) {
            if (ContentType === 0) { // Reasoning
                if (!reasoningMessageId) {
                    reasoningMessageId = MessageId;
                    yield { type: 'reasoning_started', payload: { messageId: MessageId, chunk: ContentChunk } };
                } else {
                    yield { type: 'reasoning_append', payload: { messageId: reasoningMessageId, chunk: ContentChunk } };
                }
            } else { // Answer
                if (!answerMessageId) {
                    answerMessageId = MessageId;
                    yield { type: 'answer_started', payload: { messageId: MessageId, chunk: ContentChunk } };
                } else {
                    yield { type: 'answer_append', payload: { messageId: answerMessageId, chunk: ContentChunk } };
                }
            }
        }

        if (IsDone) {
            yield { type: 'stream_done' };
            return;
        }
      } catch (e) {
        console.error("Error parsing JSON chunk:", e, "Chunk was:", line);
      }
    }
  }
}