import { ref, readonly } from 'vue';
import { fetchCurrentUser } from '../services/backofficeApi';

const currentUser = ref(null);
const loading = ref(false);
const initialized = ref(false);

export function useBackofficeAuth() {
  const loadCurrentUser = async (token) => {
    if (initialized.value) return;
    loading.value = true;
    try {
      currentUser.value = await fetchCurrentUser(token);
      initialized.value = true;
    } catch (e) {
      console.warn('Failed to load current user info', e);
    } finally {
      loading.value = false;
    }
  };

  const isTeacher = () => currentUser.value?.role === 'Teacher';
  const isParentNetworkStaff = () => currentUser.value?.role === 'ParentNetworkStaff';

  const canViewPhotos = () => currentUser.value?.capabilities?.canViewPhotos ?? false;
  const canViewDocuments = () => currentUser.value?.capabilities?.canViewDocuments ?? false;
  const canUpdateReviewStatus = () => currentUser.value?.capabilities?.canUpdateReviewStatus ?? false;
  const canEditInternalNote = () => currentUser.value?.capabilities?.canEditInternalNote ?? false;
  const canViewFullProfile = () => currentUser.value?.capabilities?.canViewFullProfile ?? false;
  const isReadOnly = () => currentUser.value?.isReadOnly ?? true;

  return {
    currentUser: readonly(currentUser),
    loading: readonly(loading),
    loadCurrentUser,
    isTeacher,
    isParentNetworkStaff,
    canViewPhotos,
    canViewDocuments,
    canUpdateReviewStatus,
    canEditInternalNote,
    canViewFullProfile,
    isReadOnly
  };
}
