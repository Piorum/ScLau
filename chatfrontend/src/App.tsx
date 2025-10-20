import React, { useState, useRef, useEffect, useMemo } from 'react';
import './App.css';
import './ChatMessage.css';
import './ChatHistory.css';
import ChatHistory from './ChatHistory';
import TopBar from './components/TopBar';
import SideMenu from './components/SideMenu';
import SettingsMenu from './components/SettingsMenu';
import ScrollToBottomButton from './components/ScrollToBottomButton';
import { useChat } from './hooks/useChat';
import { useTheme } from './context/ThemeContext';

function App() {
  const [inputValue, setInputValue] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const {
    messages, 
    isAiResponding, 
    sendMessage, 
    deleteMessage, 
    editMessage,
    branch,
    regenerate,
    chats,
    activeChatId: chatId,
    loadChats,
    loadChatHistory,
    startNewChat,
    updateChatTitle,
    deleteChat
  } = useChat();
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const [showScrollToBottom, setShowScrollToBottom] = useState(false);
  const { theme } = useTheme();

  const chatTitle = useMemo(() => {
    if (!chatId) return "New Chat";
    const chat = chats.find(c => c.chatId === chatId);
    if (!chat) return "New Chat";
    return chat.title || `Chat ${new Date(chat.lastMessage * 1000).toLocaleString()}`;
  }, [chatId, chats]);

  const handleScroll = (event: React.UIEvent<HTMLDivElement>) => {
    const { scrollTop } = event.currentTarget;
    setShowScrollToBottom(scrollTop < -200);
  };

  const scrollToBottom = () => {
    if (chatContainerRef.current) {
      chatContainerRef.current.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  useEffect(() => {
    loadChats();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const existingLink = document.getElementById('highlight-theme');
    if (existingLink) {
      document.head.removeChild(existingLink);
    }

    const link = document.createElement('link');
    link.id = 'highlight-theme';
    link.rel = 'stylesheet';
    link.href = theme === 'dark' 
      ? 'https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github-dark.min.css' 
      : 'https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github.min.css';
    document.head.appendChild(link);

    return () => {
      const linkTag = document.getElementById('highlight-theme');
      if (linkTag) {
        document.head.removeChild(linkTag);
      }
    };
  }, [theme]);





  // Auto-resize textarea height
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, [inputValue]);

  const toggleMenu = () => {
    setIsMenuOpen(!isMenuOpen);
  };

  const closeMenu = () => {
    setIsMenuOpen(false);
  };

  const openSettings = () => {
    setIsSettingsOpen(true);
  };

  const closeSettings = () => {
    setIsSettingsOpen(false);
  };

  const handleSendMessage = () => {
    if (!inputValue.trim()) return;
    sendMessage(inputValue);
    setInputValue('');
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputValue(e.target.value);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault(); // Prevent default newline
      handleSendMessage();
    } else if (e.key === 'Enter' && e.shiftKey) {
      // Allow default newline behavior for Shift+Enter
    }
  };

  const handleSelectChat = (selectedChatId: string) => {
    if (selectedChatId !== chatId) {
        loadChatHistory(selectedChatId);
    }
    if (window.innerWidth < 768) {
      closeMenu();
    }
  };

  const handleNewChat = () => {
    startNewChat();
    if (window.innerWidth < 768) {
      closeMenu();
    }
  };

  return (
    <div className="App">
      <SideMenu 
        isOpen={isMenuOpen} 
        onClose={closeMenu} 
        onSettingsClick={openSettings}
        chats={chats}
        onChatSelect={handleSelectChat}
        onNewChat={handleNewChat}
        currentChatId={chatId}
        updateChatTitle={updateChatTitle}
        deleteChat={deleteChat}
      />
      <SettingsMenu isOpen={isSettingsOpen} onClose={closeSettings} />
      <div className="main-content-wrapper">
        <TopBar onMenuToggle={toggleMenu} title={chatTitle} />
        <div className="chat-area-wrapper">

            <ChatHistory 
              ref={chatContainerRef}
              messages={messages} 
              isAiResponding={isAiResponding} 
              deleteMessage={deleteMessage} 
              editMessage={editMessage} 
              branch={branch}
              regenerate={regenerate}
              onScroll={handleScroll}
            />
            <ScrollToBottomButton onClick={scrollToBottom} isVisible={showScrollToBottom} />
            <div className="input-area">
              <textarea
                ref={textareaRef}
                value={inputValue}
                onChange={handleInputChange}
                onKeyDown={handleKeyDown}
                placeholder="Message gpt-oss:20b"
                rows={1}
              />
              <button onClick={handleSendMessage} disabled={!inputValue.trim()}>Send</button>
            </div>
        </div>
      </div>
    </div>
  );
}

export default App;
