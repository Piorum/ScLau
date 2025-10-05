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
    lastMessage: number;
}

export interface BackendMessage {
    messageId: string;
    role: number;
    content: string;
}

export interface ChatHistory {
    messages: BackendMessage[];
}