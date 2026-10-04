import { createContext, useContext, useState } from 'react';
import api from './api';

const AuthContext = createContext(null);
export const useAuth = () => useContext(AuthContext);

export const homeFor = (role) => (role === 'Admin' ? '/admin' : role === 'Provider' ? '/provider' : '/customer');

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try { return JSON.parse(localStorage.getItem('hf_user')); } catch { return null; }
  });

  const save = (data) => {
    localStorage.setItem('hf_token', data.token);
    localStorage.setItem('hf_user', JSON.stringify(data.user));
    setUser(data.user);
    return data.user;
  };

  const login = async (email, password) => save((await api.post('/auth/login', { email, password })).data);
  const register = async (payload) => save((await api.post('/auth/register', payload)).data);
  const updateUser = (u) => { localStorage.setItem('hf_user', JSON.stringify(u)); setUser(u); };
  const logout = () => {
    localStorage.removeItem('hf_token');
    localStorage.removeItem('hf_user');
    setUser(null);
  };

  return <AuthContext.Provider value={{ user, login, register, logout, updateUser }}>{children}</AuthContext.Provider>;
}
