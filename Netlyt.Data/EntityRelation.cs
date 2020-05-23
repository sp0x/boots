using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Netlyt.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The type of the entity to compile the relations to</typeparam>
    public class EntityRelation<T> : IEntityRelation<T>
    {
        private Type mTargetType;
        //<ExpressionWrap<Func<Object, Expression<Func<T, bool>>>>>
        private Dictionary<Type, IList> mArgExpressions;

        public Type DeclaredType => mTargetType;
        private uint mComplexion;

        public EntityRelation(Type tEntity)
        {
            mTargetType = tEntity;
            mArgExpressions = new Dictionary<Type, IList>();
            //new Dictionary<Type, List<ExpressionWrap<Func<object, Expression<Func<T, bool>>>>>>();
        }



        public Expression<Func<TX, bool>> Compile<TX>(object filterObj) where TX : class
        {
            return null;
        }

        public void Add<T1>(Func<T1, Expression<Func<object, bool>>> predicate)
        {
            throw new NotImplementedException("This is a stub method");
        }


        public IEntityRelation<T> Or<TValue>(Func<TValue, Expression<Func<T, bool>>> predicate)
        { 
            var key = typeof (TValue);
            var pExp = new ExpressionWrap<Func<TValue, Expression<Func<T, bool>>>>(predicate);
            pExp.LogicOp = LogicOpType.Or;
            return AddEntityRelation<TValue>(key, pExp);
        }
        public IEntityRelation<T> And<TValue>(Func<TValue, Expression<Func<T, bool>>> predicate)
        {
            var key = typeof(TValue);
            var pExp = new ExpressionWrap<Func<TValue, Expression<Func<T, bool>>>>(predicate);
            pExp.LogicOp = LogicOpType.And;
            return AddEntityRelation<TValue>(key, pExp);
        }

        private IEntityRelation<T> AddEntityRelation<TValue>(Type key, ExpressionWrap<Func<TValue, Expression<Func<T, bool>>>> pExp)
        {
            List<ExpressionWrap<Func<TValue, Expression<Func<T, bool>>>>> expList = null;
            if (mArgExpressions.ContainsKey(key))
                expList = (List < ExpressionWrap < Func < TValue, Expression < Func < T, bool>>>>> )mArgExpressions[key];
            if (expList == null) expList = new List<ExpressionWrap<Func<TValue, Expression<Func<T, bool>>>>>();
            expList.Add(pExp);
            mArgExpressions[key] = expList;
            Complexion++;
            return this;
        }


         

        /// <summary>
        /// Compiles the expressions with the targeted argument type.
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="filterObj"></param>
        /// <returns></returns>
        public Expression<Func<T, bool>> Compile<TArg>(TArg filterObj)
        {
            var targType = typeof(TArg);
            if (mArgExpressions.ContainsKey(targType))
            {
                List< ExpressionWrap < Func < TArg, Expression < Func < T, bool>>>>> relations = 
                    (List<ExpressionWrap<Func<TArg, Expression<Func<T, bool>>>>>)mArgExpressions[targType];
                Expression<Func<T, bool>> generalExp=null;


                foreach (var expW in relations)
                {
                    Expression<Func<T, bool>> expVal = expW.Expression(filterObj);
                    if (generalExp != null)
                    {
                        switch (expW.LogicOp)
                        {
                            case LogicOpType.And:
                                generalExp = generalExp.And(expVal);
                                break;
                            case LogicOpType.Or:
                                generalExp = generalExp.Or(expVal);
                                break;
                        }
                    }
                    else
                    {
                        generalExp = expVal;
                    }
                    
                } 
                return generalExp;
            }
            else return null;
        }
         

        public uint Complexion
        {
            get
            {
                return mComplexion;
            }
            private set
            {
                mComplexion = value;
            }
        }
    }
}