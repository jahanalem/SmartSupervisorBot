import { useEffect, useState } from 'react';
import axios from 'axios';

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

interface Group {
  groupId: string;
  groupName: string;
  language: string;
  isActive: boolean;
  creditPurchased: number;
  creditUsed: number;
  createdDate: string;
}

const API_BASE_URL = 'http://localhost:5078';

function App() {
  const [groups, setGroups] = useState<Group[]>([]);
  const [error, setError] = useState<string | null>(null);

  // READ: Fetch data from the server
  const fetchGroups = () => {
    axios.get<ApiGroupInfoDto[]>(`${API_BASE_URL}/groups`)
      .then(response => {
        const mappedData: Group[] = response.data.map(item => ({
          groupId: item.groupId,
          groupName: item.groupInfo?.group_name || 'N/A',
          language: item.groupInfo?.language || 'Deutsch',
          isActive: item.groupInfo?.is_active ?? false,
          creditPurchased: item.groupInfo?.credit_purchased ?? 0,
          creditUsed: item.groupInfo?.credit_used ?? 0,
          createdDate: item.groupInfo?.created_date || ''
        }));
        setGroups(mappedData);
        setError(null);
      })
      .catch(err => {
        console.error("Error fetching groups:", err);
        setError("Failed to load groups. Ensure the C# WebApi is running.");
      });
  };

  useEffect(() => {
    fetchGroups();
  }, []);

  // UPDATE: Toggle active status
  const handleToggleActive = (groupId: string, currentStatus: boolean) => {
    const newStatus = !currentStatus;
    axios.patch(`${API_BASE_URL}/groups/${groupId}/active?isActive=${newStatus}`)
      .then(() => fetchGroups())
      .catch(err => {
        console.error("Error updating status:", err);
        alert("Failed to update status.");
      });
  };

  // UPDATE: Change target language
  const handleLanguageChange = (groupId: string, newLanguage: string) => {
    axios.put(`${API_BASE_URL}/groups/${groupId}/language?language=${newLanguage}`)
      .then(() => fetchGroups())
      .catch(err => {
        console.error("Error updating language:", err);
        alert("Failed to update language.");
      });
  };

  // UPDATE: Add credit to balance
  const handleAddCredit = (groupId: string) => {
    const amountStr = window.prompt("Enter the amount of credit to add:");
    if (!amountStr) return; // User canceled

    const amount = parseFloat(amountStr);
    if (isNaN(amount) || amount <= 0) {
      alert("Please enter a valid positive number.");
      return;
    }

    axios.post(`${API_BASE_URL}/groups/${groupId}/credit?creditAmount=${amount}`)
      .then(() => fetchGroups())
      .catch(err => {
        console.error("Error adding credit:", err);
        alert("Failed to add credit.");
      });
  };

  // DELETE: Remove a group entirely
  const handleDelete = (groupId: string) => {
    if (!window.confirm(`Are you sure you want to delete group ${groupId}?`)) return;

    axios.delete(`${API_BASE_URL}/groups/${groupId}`)
      .then(() => fetchGroups())
      .catch(err => {
        console.error("Error deleting group:", err);
        alert("Failed to delete group.");
      });
  };

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
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {groups.map((group) => (
            <tr key={group.groupId}>
              <td>{group.groupId}</td>
              <td>{group.groupName}</td>
              
              {/* Language Dropdown (Inline Update) */}
              <td>
                <select title='language'
                  value={group.language} 
                  onChange={(e) => handleLanguageChange(group.groupId, e.target.value)}
                >
                  <option value="Deutsch">Deutsch</option>
                  <option value="Englisch">Englisch</option>
                  <option value="Persisch">Persisch</option>
                  <option value="Spanisch">Spanisch</option>
                  <option value="Französisch">Französisch</option>
                  <option value="Arabisch">Arabisch</option>
                  <option value="Russisch">Russisch</option>
                  <option value="Chinesisch">Chinesisch</option>
                </select>
              </td>

              <td>
                <span style={{ marginRight: '10px' }}>
                  {group.isActive ? "🟢 Active" : "🔴 Inactive"}
                </span>
                <button onClick={() => handleToggleActive(group.groupId, group.isActive)}>
                  {group.isActive ? "Deactivate" : "Activate"}
                </button>
              </td>

              {/* Credit View and Add Button */}
              <td>
                <span style={{ marginRight: '10px' }}>
                  {group.creditPurchased} / {group.creditUsed}
                </span>
                <button onClick={() => handleAddCredit(group.groupId)}>
                  + Add Credit
                </button>
              </td>

              <td>{group.createdDate ? new Date(group.createdDate).toLocaleString() : 'N/A'}</td>
              
              <td>
                <button 
                  onClick={() => handleDelete(group.groupId)}
                  style={{ backgroundColor: '#ff4d4d', color: 'white', border: 'none', padding: '5px 10px', borderRadius: '3px', cursor: 'pointer' }}
                >
                  Delete Group
                </button>
              </td>
            </tr>
          ))}
          {groups.length === 0 && !error && (
            <tr>
              <td colSpan={7}>No groups found. Add the bot to a Telegram group to register it.</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

export default App;