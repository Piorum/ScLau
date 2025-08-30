import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import './App.css';

function App() {
  const [message, setMessage] = useState('');

  const fetchData = async () => {
    setMessage('');
    const response = await fetch('/api/data');
    const reader = response.body?.getReader();
    const decoder = new TextDecoder();

    if (reader) {
      const read = async () => {
        const { done, value } = await reader.read();
        if (done) {
          return;
        }
        const chunk = decoder.decode(value, { stream: true });
        setMessage(prevMessage => prevMessage + chunk);
        read();
      };
      read();
    }
  };

  return (
    <div className="App">
      <header className="App-header">
        <div className="markdown-container">
          <ReactMarkdown>{message}</ReactMarkdown>
        </div>
        <button onClick={fetchData}>Fetch Data</button>
      </header>
    </div>
  );
}

export default App;
