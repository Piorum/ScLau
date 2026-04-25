import React, {
  useState,
  useRef,
  useEffect,
  useCallback,
  useMemo,
} from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import rehypeKatex from 'rehype-katex';
import rehypeHighlight from 'rehype-highlight';
import twemoji from 'twemoji';
import './ChatMessage.css';
import CollapsibleMessage from './components/CollapsibleMessage';
import MessageActions from './components/MessageActions';
import EditableMessageContent from './components/EditableMessageContent';
import { Message } from './types';

interface ChatMessageProps {
  message: Message;
  deleteMessage: (messageId: string | string[]) => void;
  editMessage: (
    messageId: string,
    newContent: string | { partId: string; newText: string }[]
  ) => void;
  branch: (messageId: string) => void;
  regenerate: (messageId: string) => void;
}

/* ------------------------------------------------------------------ */
/* Main component – memoised so unchanged messages never re‑render   */
/* ------------------------------------------------------------------ */
const ChatMessage: React.FC<ChatMessageProps> = ({
  message,
  deleteMessage,
  editMessage,
  branch,
  regenerate,
}) => {
  /* ------------------------------------------------------------------
     Local UI state
  ------------------------------------------------------------------ */
  const [isEditing, setIsEditing] = useState(false);
  const [isSelected, setIsSelected] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  const contentRef = useRef<HTMLDivElement>(null);
  const messageRef = useRef<HTMLDivElement>(null);

  /* ------------------------------------------------------------------
     1️⃣  Detect mobile breakpoint – we only need ONE listener for the
         whole app, but keeping it here is fine for a quick win.
  ------------------------------------------------------------------ */
  useEffect(() => {
    const mq = window.matchMedia('(max-width: 768px)');
    setIsMobile(mq.matches);
    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches);
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, []);

  /* ------------------------------------------------------------------
     2️⃣  Emoji parsing – run only when the raw text changes.
         The result is cached in `emojiHtml` and rendered with
         `dangerouslySetInnerHTML` (safe because twemoji only produces
         SVG <img> tags).
  ------------------------------------------------------------------ */
  const [emojiHtml, setEmojiHtml] = useState<string>('');
  useEffect(() => {
    // Create a temporary element so twemoji can replace Unicode emoji.
    const tmp = document.createElement('div');
    tmp.textContent = message.text; // plain text first
    twemoji.parse(tmp, { folder: 'svg', ext: '.svg' });
    setEmojiHtml(tmp.innerHTML);
  }, [message.text]);

  /* ------------------------------------------------------------------
     3️⃣  Markdown → React rendering – memoised per message text.
  ------------------------------------------------------------------ */
  const renderedMarkdown = useMemo(() => {
    return (
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkMath]}
        rehypePlugins={[rehypeKatex, rehypeHighlight]}
      >
        {message.text}
      </ReactMarkdown>
    );
  }, [message.text]);

  /* ------------------------------------------------------------------
     4️⃣  Click‑outside handling (mobile selection)
  ------------------------------------------------------------------ */
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (isMobile && messageRef.current && !messageRef.current.contains(e.target as Node)) {
        setIsSelected(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isMobile]);

  /* ------------------------------------------------------------------
     Action callbacks – wrapped in `useCallback` so the parent memo
     does not think they change on every render.
  ------------------------------------------------------------------ */
  const handleEdit = useCallback(() => setIsEditing(true), []);
  const handleDelete = useCallback(() => deleteMessage(message.id), [deleteMessage, message.id]);
  const handleBranch = useCallback(() => branch(message.id), [branch, message.id]);
  const handleRegenerate = useCallback(() => regenerate(message.id), [regenerate, message.id]);

  const handleSave = useCallback(
    (newText: string) => {
      editMessage(message.id, newText);
      setIsEditing(false);
    },
    [editMessage, message.id]
  );
  const handleCancel = useCallback(() => setIsEditing(false), []);

  const handleWrapperClick = useCallback(() => {
    if (isMobile) setIsSelected(prev => !prev);
  }, [isMobile]);

  /* ------------------------------------------------------------------
     Rendering
  ------------------------------------------------------------------ */
  if (isEditing) {
    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
        <div className={`chat-message ${message.sender} editing`}>
          <EditableMessageContent
            initialText={message.text}
            onSave={handleSave}
            onCancel={handleCancel}
          />
        </div>
      </div>
    );
  }

  // ----------  Reasoning group (collapsible) ----------
  if (message.sender === 'ai-reasoning') {
    const handleGroupEdit = (edits: { partId: string; newText: string }[]) => {
      editMessage(message.id, edits);
    };
    const handleGroupDelete = () => {
      const partIds = message.parts?.map(p => p.id) ?? [];
      if (partIds.length) deleteMessage(partIds);
    };

    return (
      <div className={`chat-message-wrapper ${message.sender}`}>
        <CollapsibleMessage
          message={message}
          onEdit={handleGroupEdit}
          onDelete={handleGroupDelete}
          onBranch={handleBranch}
          onRegenerate={handleRegenerate}
        />
      </div>
    );
  }

  // ----------  Normal message ----------
  return (
    <div
      className={`chat-message-wrapper ${message.sender}`}
      onClick={handleWrapperClick}
      ref={messageRef}
    >
      <div
        className={`chat-message ${message.sender} ${
          isMobile && isSelected ? 'selected' : ''
        }`}
      >
        {/* Show the action icons only for non‑streaming messages */}
        {!message.isStreaming && (
          <MessageActions
            onEdit={handleEdit}
            onDelete={handleDelete}
            onBranch={handleBranch}
            onRegenerate={handleRegenerate}
          />
        )}

        {/* Emoji‑parsed HTML (cached) */}
        <div ref={contentRef} dangerouslySetInnerHTML={{ __html: emojiHtml }} />

        {/* Fallback to markdown if the message contains formatting that
            isn’t covered by emojis (e.g., code blocks, tables) */}
        <div className="markdown-content">{renderedMarkdown}</div>
      </div>
    </div>
  );
};

/* ------------------------------------------------------------------ */
/* Export a memoised version – unchanged props = no re‑render           */
/* ------------------------------------------------------------------ */
export default React.memo(ChatMessage);