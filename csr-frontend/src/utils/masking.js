export const maskPhone = (phone) => {
  if (!phone || phone.length < 10) return phone;
  return phone.replace(/(\d{3})\d{4}(\d{3})/, '$1-XXX-$2');
};

export const maskFullName = (name) => {
  if (!name) return '';
  const parts = name.split(' ');
  if (parts.length < 2) return name.substring(0, 3) + '****';
  return `${parts[0]} ${parts[1].substring(0, 1)}*******`;
};
