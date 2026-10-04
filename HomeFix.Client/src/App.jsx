import { Navigate, Route, Routes } from 'react-router-dom';
import { PublicLayout, ProtectedRoute } from './layout.jsx';
import Home from './pages/Home.jsx';
import { Login, Register } from './pages/Auth.jsx';
import { About, ProviderDetails, ServicesPage, SearchPage, AiSearchPage } from './pages/Public.jsx';
import { CustomerDashboard, CustomerProfile, MyBookings, BookingDetails, ReviewPage } from './pages/Customer.jsx';
import {
  ProviderDashboard, ProviderProfilePage, MyServices, ServiceForm, Availability, BookingRequests, Jobs, ProviderReviews
} from './pages/Provider.jsx';
import { AdminDashboard, Approvals, Categories, Users, AdminBookings, Activity } from './pages/Admin.jsx';

export default function App() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/services" element={<ServicesPage />} />
        <Route path="/search" element={<SearchPage />} />
        <Route path="/ai-search" element={<AiSearchPage />} />
        <Route path="/providers/:id" element={<ProviderDetails />} />
        <Route path="/about" element={<About />} />
      </Route>

      <Route path="/customer" element={<ProtectedRoute role="Customer" />}>
        <Route index element={<CustomerDashboard />} />
        <Route path="profile" element={<CustomerProfile />} />
        <Route path="search" element={<SearchPage />} />
        <Route path="ai-search" element={<AiSearchPage />} />
        <Route path="bookings" element={<MyBookings />} />
        <Route path="bookings/:id" element={<BookingDetails />} />
        <Route path="review/:bookingId" element={<ReviewPage />} />
      </Route>

      <Route path="/provider" element={<ProtectedRoute role="Provider" />}>
        <Route index element={<ProviderDashboard />} />
        <Route path="profile" element={<ProviderProfilePage />} />
        <Route path="services" element={<MyServices />} />
        <Route path="services/new" element={<ServiceForm />} />
        <Route path="services/:id/edit" element={<ServiceForm />} />
        <Route path="availability" element={<Availability />} />
        <Route path="requests" element={<BookingRequests />} />
        <Route path="jobs" element={<Jobs />} />
        <Route path="reviews" element={<ProviderReviews />} />
      </Route>

      <Route path="/admin" element={<ProtectedRoute role="Admin" />}>
        <Route index element={<AdminDashboard />} />
        <Route path="approvals" element={<Approvals />} />
        <Route path="categories" element={<Categories />} />
        <Route path="users" element={<Users />} />
        <Route path="bookings" element={<AdminBookings />} />
        <Route path="activity" element={<Activity />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
