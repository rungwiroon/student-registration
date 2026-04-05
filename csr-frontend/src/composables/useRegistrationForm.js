import { reactive, ref } from 'vue';
import { fetchMyProfile, submitRegistration } from '../services/registrationApi';

function createFormState() {
  return {
    student: {
      studentId: '',
      name: '',
      oldRoom: '',
      oldNo: null,
      newRoom: 'ม.1/2',
      newNo: null,
      phone: '',
      bloodType: '',
      dob: ''
    },
    guardian: {
      relationType: '',
      name: '',
      phone: '',
      occupation: '',
      email: ''
    }
  };
}

function applyProfileToForm(form, profile) {
  form.student.studentId = profile.student.studentId ?? '';
  form.student.name = profile.student.name ?? '';
  form.student.newRoom = profile.student.newRoom ?? profile.student.room ?? 'ม.1/2';
  form.student.newNo = profile.student.newNo ?? null;
  form.student.phone = profile.student.phone ?? '';
  form.guardian.name = profile.guardian.name ?? '';
  form.guardian.relationType = profile.guardian.relationType ?? '';
  form.guardian.phone = profile.guardian.phone ?? '';
  form.guardian.occupation = profile.guardian.occupation ?? '';
  form.guardian.email = profile.guardian.email ?? '';
}

export function useRegistrationForm() {
  const form = reactive(createFormState());
  const photos = reactive({
    studentPhoto: null,
    guardianPhoto: null
  });
  const existingPhotoUrls = reactive({
    student: null,
    guardian: null
  });
  const isSubmitting = ref(false);
  const isProfileLoading = ref(false);

  const setStudentPhoto = (file) => {
    photos.studentPhoto = file ?? null;
  };

  const setGuardianPhoto = (file) => {
    photos.guardianPhoto = file ?? null;
  };

  const loadExistingProfile = async (token) => {
    isProfileLoading.value = true;

    try {
      const profile = await fetchMyProfile(token);
      applyProfileToForm(form, profile);
      existingPhotoUrls.student = profile.student.photoUrl ?? null;
      existingPhotoUrls.guardian = profile.guardian.photoUrl ?? null;
      return profile;
    } finally {
      isProfileLoading.value = false;
    }
  };

  const submitRegistrationForm = async (token) => {
    if (isSubmitting.value) {
      return null;
    }

    isSubmitting.value = true;

    try {
      return await submitRegistration(token, form, photos);
    } finally {
      isSubmitting.value = false;
    }
  };

  return {
    form,
    photos,
    existingPhotoUrls,
    isSubmitting,
    isProfileLoading,
    loadExistingProfile,
    submitRegistrationForm,
    setStudentPhoto,
    setGuardianPhoto
  };
}
