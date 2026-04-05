import { ref } from 'vue';
import { apiJson, apiBlob } from '../services/apiClient';

export function useIntroductionDocument() {
  const document = ref(null);
  const isLoading = ref(false);
  const studentPhotoUrl = ref(null);
  const guardianPhotoUrls = ref({});

  const fetchDocument = async (token) => {
    isLoading.value = true;
    try {
      document.value = await apiJson('/api/me/introduction-document', token);
      
      // Fetch photos
      if (document.value.student?.hasPhoto) {
        studentPhotoUrl.value = await fetchPhoto('/api/me/student-photo', token);
      }
      
      if (document.value.guardians) {
        for (const guardian of document.value.guardians) {
          if (guardian.hasPhoto) {
            const url = await fetchPhoto(`/api/me/guardian-photo/${guardian.order}`, token);
            if (url) {
              guardianPhotoUrls.value[guardian.order] = url;
            }
          }
        }
      }
      
      return document.value;
    } finally {
      isLoading.value = false;
    }
  };

  const fetchPhoto = async (path, token) => {
    try {
      const blob = await apiBlob(path, token);
      return URL.createObjectURL(blob);
    } catch (error) {
      if (error.status === 404) {
        return null;
      }
      throw error;
    }
  };

  const getGuardianPhotoUrl = (order) => {
    return guardianPhotoUrls.value[order] || null;
  };

  const printDocument = () => {
    window.print();
  };

  return {
    document,
    isLoading,
    studentPhotoUrl,
    guardianPhotoUrls,
    fetchDocument,
    getGuardianPhotoUrl,
    printDocument
  };
}
