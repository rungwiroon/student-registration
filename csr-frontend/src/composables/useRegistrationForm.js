import { reactive, ref } from 'vue';
import { fetchMyProfile, submitRegistration } from '../services/registrationApi';
import { required, phone, email, positiveInteger, parseApiToDob, parseDobToApi } from '../utils/validators';

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
      dob: null
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
  form.student.dob = parseApiToDob(profile.student.dob);
  
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

function createEmptyErrors() {
  return {
    student: {
      studentId: '',
      firstName: '',
      lastName: '',
      newNo: ''
    },
    guardians: [
      { firstName: '', lastName: '', relationType: '', phone: '', email: '' },
      { firstName: '', lastName: '', relationType: '', phone: '', email: '' }
    ]
  };
}

export function useRegistrationForm() {
  const form = reactive(createFormState());
  const errors = reactive(createEmptyErrors());
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

  // --- Validation ---

  function validateStudent() {
    let valid = true;
    errors.student.studentId = required(form.student.studentId, 'รหัสประจำตัว');
    errors.student.firstName = required(form.student.firstName, 'ชื่อ');
    errors.student.lastName = required(form.student.lastName, 'นามสกุล');
    errors.student.newNo = positiveInteger(form.student.newNo, 'เลขที่', 50);
    if (errors.student.studentId || errors.student.firstName || errors.student.lastName || errors.student.newNo) valid = false;
    return valid;
  }

  function validateGuardian(index) {
    let valid = true;
    const g = form.guardians[index];
    const e = errors.guardians[index];

    e.firstName = required(g.firstName, 'ชื่อ');
    e.lastName = required(g.lastName, 'นามสกุล');
    e.relationType = required(g.relationType, 'ความสัมพันธ์');
    e.phone = required(g.phone, 'เบอร์โทรศัพท์') || phone(g.phone);
    e.email = email(g.email);

    if (e.firstName || e.lastName || e.relationType || e.phone || e.email) valid = false;
    return valid;
  }

  function validateOptionalGuardian() {
    let valid = true;
    const g = form.guardians[1];
    const e = errors.guardians[1];

    // Only validate format, don't require fields
    e.firstName = '';
    e.lastName = '';
    e.relationType = '';
    e.phone = g.phone ? phone(g.phone) : '';
    e.email = email(g.email);

    if (e.phone || e.email) valid = false;
    return valid;
  }

  function validateForm() {
    const studentOk = validateStudent();
    const guardian1Ok = validateGuardian(0);
    const guardian2Ok = validateOptionalGuardian();
    return studentOk && guardian1Ok && guardian2Ok;
  }

  // --- Profile loading ---

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
    errors,
    photos,
    existingPhotoUrls,
    isSubmitting,
    isProfileLoading,
    validateForm,
    loadExistingProfile,
    submitRegistrationForm,
    setStudentPhoto,
    setGuardianPhoto
  };
}
