import type { RouteRecordRaw } from 'vue-router';

import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      icon: 'lucide:settings',
      order: 10,
      title: $t('page.system.title'),
    },
    name: 'System',
    path: '/system',
    children: [
      {
        name: 'UserManagement',
        path: '/system/user',
        component: () => import('#/views/system/user/index.vue'),
        meta: {
          icon: 'lucide:users',
          title: $t('page.system.user'),
          authority: ['user'],
        },
      },
      {
        name: 'RoleManagement',
        path: '/system/role',
        component: () => import('#/views/system/role/index.vue'),
        meta: {
          icon: 'lucide:shield-check',
          title: $t('page.system.role'),
          authority: ['role'],
        },
      },
      {
        name: 'PermissionManagement',
        path: '/system/permission',
        component: () => import('#/views/system/permission/index.vue'),
        meta: {
          icon: 'lucide:key',
          title: $t('page.system.permission'),
          authority: ['permission'],
        },
      },
      {
        name: 'AuditLogManagement',
        path: '/system/audit-log',
        component: () => import('#/views/system/audit-log/index.vue'),
        meta: {
          icon: 'lucide:clipboard-list',
          title: $t('page.system.auditLog'),
          authority: ['audit-log'],
        },
      },
      {
        name: 'LoginLogManagement',
        path: '/system/login-log',
        component: () => import('#/views/system/login-log/index.vue'),
        meta: {
          icon: 'lucide:log-in',
          title: $t('page.system.loginLog'),
          authority: ['login-log'],
        },
      },
      {
        name: 'DictManagement',
        path: '/system/dict',
        component: () => import('#/views/system/dict/index.vue'),
        meta: {
          icon: 'lucide:book-open',
          title: $t('page.system.dict'),
          authority: ['dict-type'],
        },
      },
      {
        name: 'RedisCacheManagement',
        path: '/system/redis-cache',
        component: () => import('#/views/system/redis-cache/index.vue'),
        meta: {
          icon: 'lucide:database',
          title: $t('page.system.redisCache'),
          authority: ['redis-cache'],
        },
      },
    ],
  },
];

export default routes;
