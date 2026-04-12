import { useEffect, useState } from 'react';
import axios from 'axios';

// Define the exact structure coming from the C# API
interface ApiGroupInfo {
  group_name: string;
  language: string;
  is_active: boolean;
  credit_purchased: number;
  credit_used: number;
  created_date: string;
}

interface ApiGroupInfoDto {
  groupId: string;
  groupInfo: ApiGroupInfo;
}

// Define the clean Frontend Model
interface Group {
  groupId: string;
  groupName: string;
  language: string;
  isActive: boolean;
  creditPurchased: number;
  creditUsed: number;
  createdDate: string;
}

function App() {
  const [groups, setGroups] = useState<Group[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    axios.get<ApiGroupInfoDto[]>('http://localhost:5078/groups')
      .then(response => {
        const mappedData: Group[] = response.data.map(item => ({
          groupId: item.groupId,
          groupName: item.groupInfo?.group_name || 'N/A',
          language: item.groupInfo?.language || 'N/A',
          isActive: item.groupInfo?.is_active ?? false,
          creditPurchased: item.groupInfo?.credit_purchased ?? 0,
          creditUsed: item.groupInfo?.credit_used ?? 0,
          createdDate: item.groupInfo?.created_date || ''
        }));
        
        setGroups(mappedData);
      })
      .catch(err => {
        console.error("Error fetching groups:", err);
        setError("Failed to load groups. Ensure the C# WebApi is running.");
      });
  }, []);

  return (
    <div style={{ padding: '20px', fontFamily: 'sans-serif' }}>
      <h1>SmartSupervisorBot Dashboard</h1>
      
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <table border={1} cellPadding={8} style={{ borderCollapse: 'collapse', width: '100%', textAlign: 'left' }}>
        <thead>
          <tr style={{ backgroundColor: '#f2f2f2' }}>
            <th>Group ID</th>
            <th>Name</th>
            <th>Language</th>
            <th>Status</th>
            <th>Credit (Purchased / Used)</th>
            <th>Creation Date</th>
          </tr>
        </thead>
        <tbody>
          {groups.map((group) => (
            <tr key={group.groupId}>
              <td>{group.groupId}</td>
              <td>{group.groupName}</td>
              <td>{group.language}</td>
              <td>{group.isActive ? "🟢 Active" : "🔴 Inactive"}</td>
              <td>{group.creditPurchased} / {group.creditUsed}</td>
              <td>{group.createdDate ? new Date(group.createdDate).toLocaleString() : 'N/A'}</td>
            </tr>
          ))}
          {groups.length === 0 && !error && (
            <tr>
              <td colSpan={6}>Loading groups...</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

export default App;