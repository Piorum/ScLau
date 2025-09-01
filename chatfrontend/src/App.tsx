import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import './App.css';

function App() {
  const [message, setMessage] = useState('');
  const [inputValue, setInputValue] = useState('');

  const fetchData = async () => {
    console.log('Sending message:', inputValue);

    const currentInputValue = inputValue; // Store current input value
    setInputValue(''); // Clear the input box immediately
    setMessage('Starting request...'); // Set temporary message

    try {
      const response = await fetch('/api/data', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(currentInputValue), // Use stored value
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setMessage(''); // Clear temporary message before streaming starts

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

          let braceCount = 0;
          let lastCut = 0;
          for (let i = 0; i < buffer.length; i++) {
            if (buffer[i] === '{') {
              braceCount++;
            } else if (buffer[i] === '}') {
              braceCount--;
              if (braceCount === 0) {
                const jsonStr = buffer.substring(lastCut, i + 1);
                lastCut = i + 1;
                try {
                  const parsed = JSON.parse(jsonStr);
                  if (parsed.Response) {
                    setMessage(prevMessage => prevMessage + parsed.Response);
                  }
                  if (parsed.Done) {
                    return;
                  }
                } catch (e) {
                  console.error('Error parsing JSON:', e, 'Chunk:', jsonStr);
                }
              }
            }
          }
          buffer = buffer.substring(lastCut);
          read();
        };
        read();
      }
    } catch (error) {
      console.error('Error sending data:', error);
      setMessage(`Error: ${error instanceof Error ? error.message : String(error)}`);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      fetchData();
    }
  };


  return (
    <div className="App">
      <div className="markdown-container">
        <ReactMarkdown>{message}</ReactMarkdown>
      </div>
      <div className="input-area">
        <input
          type="text"
          value={inputValue}
          onChange={handleInputChange}
          onKeyPress={handleKeyPress}
          placeholder="Type your message here..."
        />
        <button onClick={fetchData}>Send</button>
      </div>
    </div>
  );
}

export default App;