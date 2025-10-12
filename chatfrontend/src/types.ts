export interface MessagePart {
    id: string;
    text: string;
}
  
export type Sender = 'user' | 'ai-reasoning' | 'ai-answer';

export interface Message {
    id: string;
    sender: Sender;
    isStreaming?: boolean;
    text: string;
    parts?: MessagePart[];
}

export interface ChatListItem {
    chatId: string;
    title: string | null;
    lastMessage: number;
}

export interface BackendMessage {
    MessageId: string;
    Role: number;
    Content: string;
    ContentType: number;
}

export interface ChatHistory {
    messages: BackendMessage[];
}