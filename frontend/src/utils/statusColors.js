export const getStatusColor = (status) =>
  ({
    draft: 'default',
    reviewed: 'info',
    approved: 'success',
    published: 'primary',
    rejected: 'error',
    failed: 'error',
    pending: 'warning',
    processing: 'info',
    complete: 'success',
    sent: 'success',
  })[status] || 'default';
