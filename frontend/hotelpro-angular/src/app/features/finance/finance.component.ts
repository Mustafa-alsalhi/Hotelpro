import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BillingFinanceService } from '../../core/services/billing-finance.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import {
  ExpenseCategoryDto,
  ExpenseDto,
  FinancialSummaryDto,
  RevenueRecordDto
} from '../../core/models/finance.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-finance',
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
        <h1 class="hp-page-title">{{ i18n.t('nav.finance') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'الإيرادات، المصروفات التشغيلية، وصافي النتيجة' : 'Recognized revenues, operating expenses, and net result' }}</p>
      </div>
    </div>

    @if (summary(); as s) {
      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:14px;margin-bottom:20px;">
        <div class="hp-card" style="padding:16px;">
          <div style="font-size:0.8rem;color:#64748b;">Invoiced Total</div>
          <div style="font-size:1.35rem;font-weight:800;color:#0f2540;">{{ s.invoicedTotal }}</div>
        </div>
        <div class="hp-card" style="padding:16px;">
          <div style="font-size:0.8rem;color:#64748b;">Collected Revenues</div>
          <div style="font-size:1.35rem;font-weight:800;color:#15803d;">{{ s.recognizedRevenuesTotal }}</div>
        </div>
        <div class="hp-card" style="padding:16px;">
          <div style="font-size:0.8rem;color:#64748b;">Total Expenses</div>
          <div style="font-size:1.35rem;font-weight:800;color:#b91c1c;">{{ s.totalExpenses }}</div>
        </div>
        <div class="hp-card" style="padding:16px;">
          <div style="font-size:0.8rem;color:#64748b;">Net Operating Result</div>
          <div style="font-size:1.35rem;font-weight:800;color:#c9a227;">{{ s.netOperatingResult }}</div>
        </div>
      </div>
    }

    <div class="hp-card" style="padding:20px;margin-bottom:20px;">
      <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'تسجيل مصروف تشغيلي' : 'Record Expense' }}</h3>
      <form [formGroup]="expForm" (ngSubmit)="addExpense()" style="display:flex;flex-wrap:wrap;gap:12px;align-items:center;">
        <mat-form-field appearance="outline" style="min-width:190px;">
          <mat-label>Category</mat-label>
          <mat-select formControlName="categoryId">
            @for (c of categories(); track c.id) { <mat-option [value]="c.id">{{ c.name }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" style="flex:1;min-width:200px;">
          <mat-label>Description</mat-label>
          <input matInput formControlName="description" />
        </mat-form-field>
        <mat-form-field appearance="outline" style="width:130px;">
          <mat-label>Amount</mat-label>
          <input matInput type="number" formControlName="amount" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Date</mat-label>
          <input matInput type="date" formControlName="expenseDate" />
        </mat-form-field>
        <button *appHasPermission="'Finance.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="expForm.invalid">
          Add Expense
        </button>
      </form>
    </div>

    <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(380px,1fr));gap:20px;">
      <div class="hp-card" style="padding:16px;overflow-x:auto;">
        <h3 style="margin-top:0;color:#0f2540;">Expenses</h3>
        <table mat-table [dataSource]="expenses()" style="width:100%;">
          <ng-container matColumnDef="num"><th mat-header-cell *matHeaderCellDef>#</th><td mat-cell *matCellDef="let e">{{ e.expenseNumber }}</td></ng-container>
          <ng-container matColumnDef="cat"><th mat-header-cell *matHeaderCellDef>Category</th><td mat-cell *matCellDef="let e">{{ e.categoryName }}</td></ng-container>
          <ng-container matColumnDef="amt"><th mat-header-cell *matHeaderCellDef>Amount</th><td mat-cell *matCellDef="let e" style="color:#b91c1c;font-weight:700;">{{ e.amount }}</td></ng-container>
          <tr mat-header-row *matHeaderRowDef="['num','cat','amt']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['num','cat','amt'];"></tr>
        </table>
      </div>

      <div class="hp-card" style="padding:16px;overflow-x:auto;">
        <h3 style="margin-top:0;color:#0f2540;">Recognized Revenues</h3>
        <table mat-table [dataSource]="revenues()" style="width:100%;">
          <ng-container matColumnDef="date"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let r">{{ r.recognizedDate }}</td></ng-container>
          <ng-container matColumnDef="desc"><th mat-header-cell *matHeaderCellDef>Description</th><td mat-cell *matCellDef="let r">{{ r.description }}</td></ng-container>
          <ng-container matColumnDef="amt"><th mat-header-cell *matHeaderCellDef>Amount</th><td mat-cell *matCellDef="let r" style="color:#15803d;font-weight:700;">{{ r.amount }}</td></ng-container>
          <tr mat-header-row *matHeaderRowDef="['date','desc','amt']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['date','desc','amt'];"></tr>
        </table>
      </div>
    </div>
  `
})
export class FinanceComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly finance = inject(BillingFinanceService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly categories = signal<ExpenseCategoryDto[]>([]);
  readonly expenses = signal<ExpenseDto[]>([]);
  readonly revenues = signal<RevenueRecordDto[]>([]);
  readonly summary = signal<FinancialSummaryDto | null>(null);

  readonly expForm = this.fb.nonNullable.group({
    categoryId: ['', Validators.required],
    description: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    expenseDate: [new Date().toISOString().slice(0, 10), Validators.required],
    paymentMethod: [1]
  });

  ngOnInit(): void {
    this.finance.getExpenseCategories().subscribe((c) => this.categories.set(c));
    this.load();
  }

  load(): void {
    this.finance.getExpenses().subscribe((r) => this.expenses.set(r.items));
    this.finance.getRevenues().subscribe((r) => this.revenues.set(r.items));
    this.finance.getFinancialSummary().subscribe((s) => this.summary.set(s));
  }

  addExpense(): void {
    if (this.expForm.invalid) return;
    this.finance.createExpense(this.expForm.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Expense recorded');
        this.expForm.patchValue({ description: '', amount: 0 });
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }
}

