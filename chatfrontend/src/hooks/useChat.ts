import { useState } from 'react';
import { Message } from '../ChatMessage';

export const useChat = () => {
  const [messages, setMessages] = useState<Message[]>([]);

  const sendMessage = async (inputValue: string) => {
    if (!inputValue.trim()) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      text: inputValue,
      sender: 'user',
    };
    setMessages(prevMessages => [...prevMessages, userMessage]);

    // Placeholder for AI reasoning
    const reasoningId = (Date.now() + 1).toString();
    const reasoningMessage: Message = {
      id: reasoningId,
      text: 'Thinking...', 
      sender: 'ai-reasoning',
    };
    setMessages(prevMessages => [...prevMessages, reasoningMessage]);

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

      // Remove "Thinking..." message
      setMessages(prevMessages => prevMessages.filter(m => m.id !== reasoningId));

      const aiAnswerId = (Date.now() + 2).toString();
      const aiAnswerMessage: Message = {
        id: aiAnswerId,
        text: '',
        sender: 'ai-answer',
      };
      setMessages(prevMessages => [...prevMessages, aiAnswerMessage]);

      const reader = response.body?.getReader();
      const decoder = new TextDecoder();

      if (reader) {
        let buffer = '';
        const read = async () => {
          const { done, value } = await reader.read();
          if (done) {
            return;
          }
          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');

          lines.slice(0, -1).forEach(line => {
            if (line) {
              try {
                const parsed = JSON.parse(line);
                if (parsed.response) {
                  setMessages(prevMessages => prevMessages.map(m =>
                    m.id === aiAnswerId
                      ? { ...m, text: m.text + parsed.response }
                      : m
                  ));
                }
              } catch (e) {
                console.error('Error parsing JSON:', e, 'Line:', line);
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
    }
  };

  return { messages, sendMessage };
};
