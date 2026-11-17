import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HrInventoryService } from '../../core/services/hr-inventory.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { DepartmentDto, EmployeeDto } from '../../core/models/employee.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.employees') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إدارة الموظفين والأقسام والحضور والانصراف' : 'Manage hotel staff, departments, and attendance' }}</p>
      </div>
    </div>

    <div class="hp-card" style="padding:20px;margin-bottom:20px;">
      <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'إضافة موظف' : 'Add Employee' }}</h3>
      <form [formGroup]="form" (ngSubmit)="create()" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:12px;">
        <mat-form-field appearance="outline"><mat-label>Code</mat-label><input matInput formControlName="employeeCode" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Full Name</mat-label><input matInput formControlName="fullName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Phone</mat-label><input matInput formControlName="phone" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Email</mat-label><input matInput formControlName="email" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Job Title</mat-label><input matInput formControlName="jobTitle" /></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Department</mat-label>
          <mat-select formControlName="departmentId">
            @for (d of departments(); track d.id) { <mat-option [value]="d.id">{{ d.name }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Hire Date</mat-label><input matInput type="date" formControlName="hireDate" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Base Salary</mat-label><input matInput type="number" formControlName="baseSalary" /></mat-form-field>
        <div style="grid-column:1/-1;display:flex;justify-content:flex-end;">
          <button *appHasPermission="'Employees.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid">
            <mat-icon>person_add</mat-icon><span>Save Employee</span>
          </button>
        </div>
      </form>
    </div>

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      <table mat-table [dataSource]="employees()" style="width:100%;">
        <ng-container matColumnDef="code"><th mat-header-cell *matHeaderCellDef>Code</th><td mat-cell *matCellDef="let e" style="font-weight:700;">{{ e.employeeCode }}</td></ng-container>
        <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let e">{{ e.fullName }}</td></ng-container>
        <ng-container matColumnDef="dept"><th mat-header-cell *matHeaderCellDef>Department</th><td mat-cell *matCellDef="let e">{{ e.departmentName }} — {{ e.jobTitle }}</td></ng-container>
        <ng-container matColumnDef="salary"><th mat-header-cell *matHeaderCellDef>Salary</th><td mat-cell *matCellDef="let e">{{ e.baseSalary }}</td></ng-container>
        <tr mat-header-row *matHeaderRowDef="['code','name','dept','salary']"></tr>
        <tr mat-row *matRowDef="let row; columns: ['code','name','dept','salary'];"></tr>
      </table>
    </div>
  `
})
export class EmployeesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly hr = inject(HrInventoryService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly departments = signal<DepartmentDto[]>([]);
  readonly employees = signal<EmployeeDto[]>([]);

  readonly form = this.fb.nonNullable.group({
    employeeCode: ['', Validators.required],
    fullName: ['', Validators.required],
    phone: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    jobTitle: ['', Validators.required],
    departmentId: ['', Validators.required],
    hireDate: [new Date().toISOString().slice(0, 10), Validators.required],
    baseSalary: [4000, [Validators.required, Validators.min(0)]],
    status: [1]
  });

  ngOnInit(): void {
    this.hr.getDepartments().subscribe((d) => this.departments.set(d));
    this.load();
  }

  load(): void {
    this.hr.getEmployees().subscribe((r) => this.employees.set(r.items));
  }

  create(): void {
    if (this.form.invalid) return;
    this.hr.createEmployee(this.form.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Employee added');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }
}

