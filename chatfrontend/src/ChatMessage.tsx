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

/* --------------------------------------------------------------- */
/*  Main component – memoised so unchanged messages never re‑render*/
/* --------------------------------------------------------------- */
const ChatMessage: React.FC<ChatMessageProps> = ({
  message,
  deleteMessage,
  editMessage,
  branch,
  regenerate,
}) => {
  /* ----------------------------------------------------------- */
  /*  Local UI state                                             */
  /* ----------------------------------------------------------- */
  const [isEditing, setIsEditing] = useState(false);
  const [isSelected, setIsSelected] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  const contentRef = useRef<HTMLDivElement>(null);
  const messageRef = useRef<HTMLDivElement>(null);

  /* ----------------------------------------------------------- */
  /*  1️⃣  Detect mobile breakpoint                               */
  /* ----------------------------------------------------------- */
  useEffect(() => {
    const mq = window.matchMedia('(max-width: 768px)');
    setIsMobile(mq.matches);
    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches);
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, []);

  /* ----------------------------------------------------------- */
  /*  2️⃣  Render markdown – memoised per message text           */
  /* ----------------------------------------------------------- */
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

  /* ----------------------------------------------------------- */
  /*  3️⃣  After the markdown is in the DOM, replace emojis with
          the SVG version (twemoji). This runs only when the text
          changes, not on every scroll or keystroke.               */
  /* ----------------------------------------------------------- */
  useEffect(() => {
    if (contentRef.current) {
      // `twemoji.parse` walks the subtree and swaps each Unicode emoji
      // with an <img> element that points at the CDN SVG.
      twemoji.parse(contentRef.current, {
        folder: 'svg',
        ext: '.svg',
      });
    }
  }, [message.text]); // only re‑run when the source text changes

  /* ----------------------------------------------------------- */
  /*  4️⃣  Click‑outside handling (mobile selection)             */
  /* ----------------------------------------------------------- */
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (isMobile && messageRef.current && !messageRef.current.contains(e.target as Node)) {
        setIsSelected(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isMobile]);

  /* ----------------------------------------------------------- */
  /*  5️⃣  Action callbacks – stable references                  */
  /* ----------------------------------------------------------- */
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

  /* ----------------------------------------------------------- */
  /*  Rendering                                                  */
  /* ----------------------------------------------------------- */
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
        {/* Action icons – hidden while streaming */}
        {!message.isStreaming && (
          <MessageActions
            onEdit={handleEdit}
            onDelete={handleDelete}
            onBranch={handleBranch}
            onRegenerate={handleRegenerate}
          />
        )}

        {/* The **single** rendered block:
            • Markdown (code, tables, LaTeX, etc.)
            • After it lands in the DOM we run twemoji.parse → emojis become SVGs
        */}
        <div ref={contentRef}>{renderedMarkdown}</div>
      </div>
    </div>
  );
};

/* --------------------------------------------------------------- */
/*  Export a memoised version – unchanged props = no re‑render      */
/* --------------------------------------------------------------- */
export default React.memo(ChatMessage);