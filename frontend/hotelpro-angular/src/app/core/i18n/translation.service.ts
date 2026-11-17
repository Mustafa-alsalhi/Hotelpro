import { Injectable, signal, computed } from '@angular/core';

export type AppLang = 'ar' | 'en';

const DICTIONARY: Record<AppLang, Record<string, string>> = {
  ar: {
    'app.title': 'HotelPro',
    'app.subtitle': 'نظام إدارة الفنادق المتكامل',
    'auth.login': 'تسجيل الدخول',
    'auth.email': 'البريد الإلكتروني',
    'auth.password': 'كلمة المرور',
    'auth.signingIn': 'جاري تسجيل الدخول...',
    'auth.welcomeBack': 'مرحباً بعودتك',
    'auth.loginSubtitle': 'سجّل الدخول للوصول إلى لوحة عمليات الفندق',
    'auth.logout': 'تسجيل الخروج',
    'auth.invalidCredentials': 'البريد الإلكتروني أو كلمة المرور غير صحيحة',
    'nav.dashboard': 'لوحة التحكم',
    'nav.operations': 'العمليات الفندقية',
    'nav.reservations': 'الحجوزات',
    'nav.calendar': 'تقويم الحجوزات',
    'nav.checkIn': 'تسجيل الوصول',
    'nav.checkOut': 'تسجيل المغادرة',
    'nav.rooms': 'الغرف',
    'nav.guests': 'النزلاء',
    'nav.guestServices': 'خدمات النزلاء',
    'nav.housekeeping': 'إدارة النظافة',
    'nav.maintenance': 'الصيانة',
    'nav.finance': 'المالية والمحاسبة',
    'nav.billing': 'الفواتير والمدفوعات',
    'nav.rates': 'الأسعار والخطط',
    'nav.hrInventory': 'الموارد والمخزون',
    'nav.employees': 'الموظفون',
    'nav.inventory': 'المخزون والمشتريات',
    'nav.reports': 'التقارير والتحليلات',
    'nav.admin': 'الإدارة والأمان',
    'nav.users': 'المستخدمون',
    'nav.roles': 'الأدوار والصلاحيات',
    'nav.settings': 'إعدادات الفندق',
    'common.search': 'بحث...',
    'common.add': 'إضافة',
    'common.edit': 'تعديل',
    'common.save': 'حفظ',
    'common.cancel': 'إلغاء',
    'common.confirm': 'تأكيد',
    'common.active': 'نشط',
    'common.inactive': 'موقوف',
    'common.status': 'الحالة',
    'common.actions': 'الإجراءات',
    'common.noData': 'لا توجد بيانات لعرضها',
    'common.loading': 'جاري التحميل...',
    'users.title': 'إدارة المستخدمين',
    'users.subtitle': 'إضافة وتعديل حسابات الموظفين وربطها بالأدوار الوظيفية',
    'users.create': 'مستخدم جديد',
    'users.fullName': 'الاسم الكامل',
    'users.phone': 'رقم الهاتف',
    'users.roles': 'الأدوار',
    'users.language': 'اللغة المفضلة',
    'users.toggleConfirm': 'هل أنت متأكد من تغيير حالة تفعيل هذا المستخدم؟',
    'roles.title': 'الأدوار والصلاحيات',
    'roles.subtitle': 'إدارة مصفوفة الصلاحيات لكل دور وظيفي في النظام',
    'roles.create': 'دور جديد',
    'roles.name': 'اسم الدور',
    'roles.description': 'الوصف',
    'roles.permissions': 'الصلاحيات',
    'roles.systemRole': 'دور نظام',
    'dashboard.welcome': 'مرحباً بك في نظام HotelPro',
    'dashboard.phase2Note': 'تم تفعيل البنية الأساسية، المصادقة عبر JWT و Refresh Tokens، ونظام الصلاحيات RBAC بنجاح. الوحدات التشغيلية ستظهر هنا تباعاً.'
  },
  en: {
    'app.title': 'HotelPro',
    'app.subtitle': 'Enterprise Hotel Management System',
    'auth.login': 'Sign In',
    'auth.email': 'Email Address',
    'auth.password': 'Password',
    'auth.signingIn': 'Signing in...',
    'auth.welcomeBack': 'Welcome Back',
    'auth.loginSubtitle': 'Sign in to access hotel operations',
    'auth.logout': 'Sign Out',
    'auth.invalidCredentials': 'Invalid email or password',
    'nav.dashboard': 'Dashboard',
    'nav.operations': 'Hotel Operations',
    'nav.reservations': 'Reservations',
    'nav.calendar': 'Booking Calendar',
    'nav.checkIn': 'Check-In',
    'nav.checkOut': 'Check-Out',
    'nav.rooms': 'Rooms',
    'nav.guests': 'Guests',
    'nav.guestServices': 'Guest Services',
    'nav.housekeeping': 'Housekeeping',
    'nav.maintenance': 'Maintenance',
    'nav.finance': 'Finance & Billing',
    'nav.billing': 'Billing & Payments',
    'nav.rates': 'Rate Plans',
    'nav.hrInventory': 'HR & Inventory',
    'nav.employees': 'Employees',
    'nav.inventory': 'Inventory & Purchasing',
    'nav.reports': 'Reports & Analytics',
    'nav.admin': 'Administration',
    'nav.users': 'Users',
    'nav.roles': 'Roles & Permissions',
    'nav.settings': 'Hotel Settings',
    'common.search': 'Search...',
    'common.add': 'Add',
    'common.edit': 'Edit',
    'common.save': 'Save',
    'common.cancel': 'Cancel',
    'common.confirm': 'Confirm',
    'common.active': 'Active',
    'common.inactive': 'Inactive',
    'common.status': 'Status',
    'common.actions': 'Actions',
    'common.noData': 'No records found',
    'common.loading': 'Loading...',
    'users.title': 'User Management',
    'users.subtitle': 'Create and manage staff accounts and role assignments',
    'users.create': 'New User',
    'users.fullName': 'Full Name',
    'users.phone': 'Phone Number',
    'users.roles': 'Roles',
    'users.language': 'Preferred Language',
    'users.toggleConfirm': 'Are you sure you want to toggle this user active status?',
    'roles.title': 'Roles & Permissions',
    'roles.subtitle': 'Configure granular permissions per role',
    'roles.create': 'New Role',
    'roles.name': 'Role Name',
    'roles.description': 'Description',
    'roles.permissions': 'Permissions',
    'roles.systemRole': 'System Role',
    'dashboard.welcome': 'Welcome to HotelPro',
    'dashboard.phase2Note': 'Foundation, JWT + Refresh Token authentication, and RBAC permission system are active. Operational modules will be connected progressively.'
  }
};

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private readonly langSignal = signal<AppLang>(this.loadInitialLang());

  readonly lang = this.langSignal.asReadonly();
  readonly isRtl = computed(() => this.langSignal() === 'ar');

  constructor() {
    this.applyDirection(this.langSignal());
  }

  setLanguage(lang: AppLang): void {
    this.langSignal.set(lang);
    localStorage.setItem('hp_lang', lang);
    this.applyDirection(lang);
  }

  toggleLanguage(): void {
    this.setLanguage(this.langSignal() === 'ar' ? 'en' : 'ar');
  }

  t(key: string): string {
    return DICTIONARY[this.langSignal()][key] ?? key;
  }

  private loadInitialLang(): AppLang {
    const saved = localStorage.getItem('hp_lang');
    return saved === 'en' ? 'en' : 'ar';
  }

  private applyDirection(lang: AppLang): void {
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }
}

