import { reactive, ref } from 'vue';
import { fetchMyProfile, submitRegistration } from '../services/registrationApi';

function createEmptyGuardian(order) {
  return {
    order,
    relationType: '',
    firstName: '',
    lastName: '',
    phone: '',
    occupation: '',
    email: ''
  };
}

function createFormState() {
  return {
    student: {
      studentId: '',
      firstName: '',
      lastName: '',
      nickname: '',
      oldRoom: '',
      oldNo: null,
      newRoom: 'ม.1/2',
      newNo: null,
      phone: '',
      bloodType: '',
      dob: ''
    },
    guardians: [
      createEmptyGuardian(1),
      createEmptyGuardian(2)
    ]
  };
}

function applyProfileToForm(form, profile) {
  form.student.studentId = profile.student.studentId ?? '';
  form.student.firstName = profile.student.firstName ?? '';
  form.student.lastName = profile.student.lastName ?? '';
  form.student.nickname = profile.student.nickname ?? '';
  form.student.newRoom = profile.student.newRoom ?? profile.student.room ?? 'ม.1/2';
  form.student.newNo = profile.student.newNo ?? null;
  form.student.phone = profile.student.phone ?? '';
  form.student.bloodType = profile.student.bloodType ?? '';
  form.student.dob = profile.student.dob ?? '';
  
  // Apply guardians from profile
  if (profile.guardians && profile.guardians.length > 0) {
    profile.guardians.forEach((g, index) => {
      if (index < form.guardians.length) {
        form.guardians[index].order = g.order ?? index + 1;
        form.guardians[index].relationType = g.relationType ?? '';
        form.guardians[index].firstName = g.firstName ?? '';
        form.guardians[index].lastName = g.lastName ?? '';
        form.guardians[index].phone = g.phone ?? '';
        form.guardians[index].occupation = g.occupation ?? '';
        form.guardians[index].email = g.email ?? '';
      }
    });
  }
}

export function useRegistrationForm() {
  const form = reactive(createFormState());
  const photos = reactive({
    studentPhoto: null,
    guardianPhoto1: null,
    guardianPhoto2: null
  });
  const existingPhotoUrls = reactive({
    student: null,
    guardian1: null,
    guardian2: null
  });
  const isSubmitting = ref(false);
  const isProfileLoading = ref(false);

  const setStudentPhoto = (file) => {
    photos.studentPhoto = file ?? null;
  };

  const setGuardianPhoto = (file, order = 1) => {
    if (order === 1) {
      photos.guardianPhoto1 = file ?? null;
    } else if (order === 2) {
      photos.guardianPhoto2 = file ?? null;
    }
  };

  const loadExistingProfile = async (token) => {
    isProfileLoading.value = true;

    try {
      const profile = await fetchMyProfile(token);
      applyProfileToForm(form, profile);
      existingPhotoUrls.student = profile.student.photoUrl ?? null;
      if (profile.guardians && profile.guardians.length > 0) {
        profile.guardians.forEach((g, index) => {
          if (index === 0) {
            existingPhotoUrls.guardian1 = g.photoUrl ?? null;
          } else if (index === 1) {
            existingPhotoUrls.guardian2 = g.photoUrl ?? null;
          }
        });
      }
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
