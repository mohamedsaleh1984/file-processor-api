import React, { useState } from 'react';
import axios from 'axios';
import { HubConnectionBuilder } from '@microsoft/signalr';

const FileUploader = () => {
    const [file, setFile] = useState(null);
    const [progress, setProgress] = useState(0);
    const [taskId, setTaskId] = useState(null);
    const [downloadUrl, setDownloadUrl] = useState(null);
    const baseUrl = "http://localhost:5000";

    const startProcessing = async (file) => {
        setFile(file);

        const formData = new FormData();
        formData.append('file', file);

        const response = await axios.post(`${baseUrl}/api/fileprocessing/upload`, formData);
        console.log('zzzzzzzzzzzz', response);

        const newTaskId = response.data.taskId;
        setTaskId(newTaskId);

        const connection = new HubConnectionBuilder()
            .withUrl(`${baseUrl}/processinghub`)
            .build();

        await connection.start();
        await connection.invoke('JoinTaskGroup', newTaskId);

        connection.on('ProgressUpdate', (percentage) => {
            console.log('ProgressUpdate ', percentage);
            setProgress(percentage);
        });

        connection.on('ProcessingFailed', (error) => {
            alert(`Processing failed: ${error}`);
            setProgress(0);
        });

        connection.on('ProcessingCompleted', () => {
            console.log('ProcessingCompleted');
            setDownloadUrl(`${baseUrl}/api/fileprocessing/download/${newTaskId}`);
            connection.stop();
        });
    }

    return (
        <div className="max-w-md mx-auto p-6 border rounded-xl shadow-lg bg-white space-y-4">
            <label className="block text-gray-700 text-sm font-bold">Upload File</label>
            <br />
            <input
                type="file"
                className="block w-full text-sm text-gray-500 border border-gray-300 rounded-lg cursor-pointer p-2"
                onChange={(e) => startProcessing(e.target.files[0])}
                disabled={!!taskId}
            />
            {file && <p className="text-sm text-gray-600">Selected: {file.name}</p>}
            {progress > 0 && (
                <div>
                    <progress value={progress} max="100" />
                    <span>{progress}%</span>
                </div>
            )}

            {downloadUrl && (
                <a href={downloadUrl} download>
                    Download Processed File
                </a>
            )}
        </div>
    );
};

export default FileUploader;