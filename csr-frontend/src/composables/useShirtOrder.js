import { reactive, ref, computed } from 'vue';

const SIZES = ['2XS', 'XS', 'S', 'M', 'L', 'XL', '2XL', '3XL', '4XL', '5XL', '6XL'];
const DESIGNS = ['A', 'B'];
const UNIT_PRICE = 250; // ฿ per shirt

function createEmptyItems() {
  const items = [];
  for (const design of DESIGNS) {
    for (const size of SIZES) {
      items.push({ design, size, quantity: 0, unitPrice: UNIT_PRICE });
    }
  }
  return items;
}

function createFormState() {
  return {
    lineDisplayName: '',
    studentName: '',
    studentNumber: '',
    items: createEmptyItems()
  };
}

function createEmptyErrors() {
  return {
    studentName: '',
    studentNumber: '',
    items: '',
    slip: ''
  };
}

export function useShirtOrder() {
  const form = reactive(createFormState());
  const errors = reactive(createEmptyErrors());
  const slipFile = ref(null);
  const slipPreviewUrl = ref(null);
  const isSubmitting = ref(false);
  const isSuccess = ref(false);
  const orderResult = ref(null);

  const totalAmount = computed(() => {
    return form.items.reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0);
  });

  const totalQuantity = computed(() => {
    return form.items.reduce((sum, item) => sum + item.quantity, 0);
  });

  const orderSummary = computed(() => {
    const byDesign = {};
    for (const item of form.items) {
      if (item.quantity > 0) {
        if (!byDesign[item.design]) byDesign[item.design] = [];
        byDesign[item.design].push(`${item.size}=${item.quantity}`);
      }
    }
    const parts = Object.entries(byDesign).map(([design, sizes]) => {
      return `แบบ ${design} (${sizes.join(', ')})`;
    });
    return parts.join('; ');
  });

  const hasItems = computed(() => totalQuantity.value > 0);

  function setQuantity(design, size, qty) {
    const item = form.items.find(i => i.design === design && i.size === size);
    if (item) {
      item.quantity = Math.max(0, qty);
    }
  }

  function increment(design, size) {
    const item = form.items.find(i => i.design === design && i.size === size);
    if (item) {
      item.quantity += 1;
    }
  }

  function decrement(design, size) {
    const item = form.items.find(i => i.design === design && i.size === size);
    if (item) {
      item.quantity = Math.max(0, item.quantity - 1);
    }
  }

  function setSlipFile(file) {
    if (slipPreviewUrl.value) {
      URL.revokeObjectURL(slipPreviewUrl.value);
      slipPreviewUrl.value = null;
    }
    slipFile.value = file ?? null;
    if (file) {
      slipPreviewUrl.value = URL.createObjectURL(file);
    }
  }

  function validateForm() {
    let valid = true;
    errors.studentName = '';
    errors.studentNumber = '';
    errors.items = '';
    errors.slip = '';

    if (!form.studentName || form.studentName.trim().length === 0) {
      errors.studentName = 'กรุณากรอกชื่อ-นามสกุลนักเรียน';
      valid = false;
    }

    const num = parseInt(form.studentNumber, 10);
    if (!form.studentNumber || isNaN(num)) {
      errors.studentNumber = 'กรุณากรอกเลขที่';
      valid = false;
    } else if (num < 1 || num > 40) {
      errors.studentNumber = 'เลขที่ต้องอยู่ระหว่าง 1-40';
      valid = false;
    }

    if (totalQuantity.value <= 0) {
      errors.items = 'กรุณาเลือกจำนวนเสื้ออย่างน้อย 1 ตัว';
      valid = false;
    }

    if (!slipFile.value) {
      errors.slip = 'กรุณาแนบสลิปการโอนเงิน';
      valid = false;
    }

    return valid;
  }

  function resetForm() {
    Object.assign(form, createFormState());
    Object.assign(errors, createEmptyErrors());
    setSlipFile(null);
    isSuccess.value = false;
    orderResult.value = null;
  }

  return {
    form,
    errors,
    slipFile,
    slipPreviewUrl,
    isSubmitting,
    isSuccess,
    orderResult,
    totalAmount,
    totalQuantity,
    orderSummary,
    hasItems,
    SIZES,
    DESIGNS,
    UNIT_PRICE,
    setQuantity,
    increment,
    decrement,
    setSlipFile,
    validateForm,
    resetForm
  };
}
