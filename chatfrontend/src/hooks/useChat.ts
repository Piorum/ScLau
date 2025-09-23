import { useState } from 'react';
import { Message } from '../ChatMessage';
import { ApiStreamParser } from '../utils/apiStreamParser';

export const useChat = () => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [activeStreamingMessageId, setActiveStreamingMessageId] = useState<string | null>(null);

  const sendMessage = (inputValue: string) => {
    if (!inputValue.trim()) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      text: inputValue,
      sender: 'user',
    };
    setMessages(prevMessages => [...prevMessages, userMessage]);

    setTimeout(() => {
      const reasoningId = (Date.now() + 1).toString();
      const reasoningMessage: Message = {
        id: reasoningId,
        text: 'Thinking...', 
        sender: 'ai-reasoning',
      };
      setMessages(prev => [...prev, reasoningMessage]);
      setActiveStreamingMessageId(reasoningId);

      const fetchAndStream = async () => {
        try {
          const response = await fetch('/api/data', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(inputValue),
          });

          if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
          }

          setMessages(prevMessages => prevMessages.filter(m => m.id !== reasoningId));

          const reader = response.body?.getReader();
          const decoder = new TextDecoder();

          if (reader) {
            let buffer = '';
            let currentChannel: string | null = null;
            let currentMessageIdForTextUpdate: string | null = reasoningId;
            
            const read = async () => {
              const { done, value } = await reader.read();
              if (done) {
                setActiveStreamingMessageId(null);
                return;
              }
              buffer += decoder.decode(value, { stream: true });
              const lines = buffer.split('\n');

              lines.slice(0, -1).forEach(line => {
                if (line) {
                  const parser = new ApiStreamParser(line);
                  const { response, channel } = parser.getData();

                  if (response) {
                    if (channel !== currentChannel) {
                      currentChannel = channel;
                      const newAiMessageId = (Date.now() + Math.random()).toString();
                      currentMessageIdForTextUpdate = newAiMessageId; // Set for text update
                      const sender = (channel === 'final') ? 'ai-answer' : 'ai-reasoning';
                      const newMessage: Message = {
                        id: newAiMessageId,
                        text: response,
                        sender: sender,
                      };
                      setMessages(prevMessages => [...prevMessages, newMessage]);
                      setActiveStreamingMessageId(newAiMessageId);
                    } else {
                      setMessages(prevMessages => prevMessages.map(m =>
                        m.id === currentMessageIdForTextUpdate // Use for text update
                          ? { ...m, text: m.text + response } : m
                      ));
                    }
                  }
                }
              });

              buffer = lines[lines.length - 1];
              read();
            };
            read();
          }
        } catch (error) {
          const errorMessage: Message = {
            id: (Date.now() + 3).toString(),
            text: `Error: ${error instanceof Error ? error.message : String(error)}`,
            sender: 'ai-reasoning',
          };
          setMessages(prevMessages => [...prevMessages.filter(m => m.id !== reasoningId), errorMessage]);
          setActiveStreamingMessageId(null); // Stop streaming on error
        }
      };

      fetchAndStream();
    }, 0);
  };

  const deleteMessage = (messageId: string) => {
    setMessages(prevMessages => prevMessages.filter(msg => msg.id !== messageId));
  };

  const editMessage = (messageId: string, newText: string) => {
    setMessages(prevMessages =>
      prevMessages.map(msg =>
        msg.id === messageId ? { ...msg, text: newText } : msg
      )
    );
  };

  const messagesWithStreamingStatus = messages.map(msg => ({
    ...msg,
    isStreaming: msg.id === activeStreamingMessageId,
  }));

  return { messages: messagesWithStreamingStatus, sendMessage, deleteMessage, editMessage };
};
