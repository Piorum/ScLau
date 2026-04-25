import React, { useMemo } from 'react';
import ChatMessage from './ChatMessage';
import LoadingIcon from './components/LoadingIcon';
import './ChatHistory.css';
import { Message } from './types';

interface ChatHistoryProps {
  messages: Message[];
  isAiResponding: boolean;
  deleteMessage: (messageId: string | string[]) => void;
  editMessage: (
    messageId: string,
    newContent: string | { partId: string; newText: string }[]
  ) => void;
  branch: (messageId: string) => void;
  regenerate: (messageId: string) => void;
  onScroll: (event: React.UIEvent<HTMLDivElement>) => void;
}

/* ------------------------------------------------------------------ */
/* Helper – build a single “reasoning” group from an array of parts   */
/* ------------------------------------------------------------------ */
function makeReasonGroup(parts: Message[]): Message {
  return {
    id: parts[0].id,
    sender: 'ai-reasoning',
    text: '',
    parts: parts.map(p => ({ id: p.id, text: p.text })),
    isStreaming: parts.some(p => p.isStreaming ?? false),
  };
}

/* ------------------------------------------------------------------ */
/* Main component                                                     */
/* ------------------------------------------------------------------ */
const ChatHistory = React.forwardRef<HTMLDivElement, ChatHistoryProps>(
  (
    {
      messages,
      isAiResponding,
      deleteMessage,
      editMessage,
      branch,
      regenerate,
      onScroll,
    },
    ref
  ) => {
    /* --------------------------------------------------------------
       1️⃣  Group & reverse the messages – memoised so it only runs
           when the *raw* `messages` array changes.
       -------------------------------------------------------------- */
    const groupedAndReversed = useMemo(() => {
      const groups: Message[] = [];
      let reasoningBuffer: Message[] = [];

      for (const msg of messages) {
        if (msg.sender === 'ai-reasoning') {
          reasoningBuffer.push(msg);
        } else {
          if (reasoningBuffer.length) {
            groups.push(makeReasonGroup(reasoningBuffer));
            reasoningBuffer = [];
          }
          groups.push(msg);
        }
      }

      // leftover reasoning messages at the end of the array
      if (reasoningBuffer.length) {
        groups.push(makeReasonGroup(reasoningBuffer));
      }

      // We want newest at the *top* of the UI → reverse once here
      return groups.slice().reverse();
    }, [messages]);

    return (
      <div className="chat-history" ref={ref} onScroll={onScroll}>
        {/* Spinner – keep it outside the mapped list so it does not
            trigger a full re‑render of all messages when it toggles. */}
        {isAiResponding && <LoadingIcon />}

        {/* Render each (already‑reversed) group */}
        {groupedAndReversed.map(message => (
          <ChatMessage
            key={message.id}
            message={message}
            deleteMessage={deleteMessage}
            editMessage={editMessage}
            branch={branch}
            regenerate={regenerate}
          />
        ))}
      </div>
    );
  }
);

export default ChatHistory;