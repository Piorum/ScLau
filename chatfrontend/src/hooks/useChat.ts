import { useState } from 'react';
import { Message } from '../ChatMessage';
import { ApiStreamParser } from '../utils/apiStreamParser';

export const useChat = () => {
  const [messages, setMessages] = useState<Message[]>([]);
  // activeStreamingMessageId tracks the ID of the message that is currently streaming.
  // This is used to derive the 'isStreaming' status for messages, which controls
  // the visibility of loading indicators and context buttons.
  const [activeStreamingMessageId, setActiveStreamingMessageId] = useState<string | null>(null);

  const sendMessage = (inputValue: string) => {
    if (!inputValue.trim()) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      text: inputValue,
      sender: 'user',
    };
    setMessages(prevMessages => [...prevMessages, userMessage]);

    // Use setTimeout to allow the UI to render the user's message and snap to bottom
    // before the 'Thinking...' message appears and streaming begins.
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
                        if (currentChannel === null) { // First chunk of AI response
                          setMessages(prevMessages => prevMessages.map(m =>
                            m.id === reasoningId ? { ...m, text: response } : m
                          ));
                          currentChannel = channel;
                          setActiveStreamingMessageId(reasoningId); // Still streaming the reasoning message
                        } else if (channel !== currentChannel) { // Channel has changed
                          // Mark previous message as not streaming
                          if (activeStreamingMessageId) {
                            setMessages(prevMessages => prevMessages.map(m =>
                              m.id === activeStreamingMessageId ? { ...m, isStreaming: false } : m
                            ));
                          }

                          currentChannel = channel;
                          const newAiMessageId = (Date.now() + Math.random()).toString();
                          currentMessageIdForTextUpdate = newAiMessageId; // Update for text appending
                          const sender = (channel === 'final') ? 'ai-answer' : 'ai-reasoning';
                          const newMessage: Message = {
                            id: newAiMessageId,
                            text: response,
                            sender: sender,
                          };
                          setMessages(prevMessages => [...prevMessages, newMessage]);
                          setActiveStreamingMessageId(newAiMessageId);
                        } else { // Same channel, append text
                          setMessages(prevMessages => prevMessages.map(m =>
                            m.id === currentMessageIdForTextUpdate
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
          // On error, no message is actively streaming.
          setActiveStreamingMessageId(null); 
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

  // Dynamically derive the 'isStreaming' status for each message based on activeStreamingMessageId.
  // This ensures that the UI correctly reflects which message is currently streaming.
  const messagesWithStreamingStatus = messages.map(msg => ({
    ...msg,
    isStreaming: msg.id === activeStreamingMessageId,
  }));

  return { messages: messagesWithStreamingStatus, sendMessage, deleteMessage, editMessage };
};